import json
import subprocess
import time
import urllib.error
import urllib.request
from datetime import datetime

DOCKER_CONTEXT = "desktop-linux"

PREFERRED_LEADER = "dct"
OTHER_NODE = "dct-replica"

RECOVERY_DELAY_SECONDS = 35
FAILBACK_STABLE_SECONDS = 5
SWITCHOVER_RETRY_SECONDS = 120
POLL_INTERVAL_SECONDS = 1

NODES = {
    "dct": "http://127.0.0.1:8008",
    "dct-replica": "http://127.0.0.1:8009",
}

HTTP = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def log(message):
    timestamp = datetime.now().astimezone().isoformat(timespec="seconds")
    print(f"[{timestamp}] {message}", flush=True)


def docker(*arguments):
    result = subprocess.run(
        ["docker", "--context", DOCKER_CONTEXT, *arguments],
        capture_output=True,
        text=True,
        check=True,
        timeout=15,
    )
    return result.stdout.strip()


def request(node, path, payload=None, timeout=2):
    body = None if payload is None else json.dumps(payload).encode("utf-8")

    http_request = urllib.request.Request(
        f"{NODES[node]}{path}",
        data=body,
        headers={"Content-Type": "application/json"},
        method="GET" if payload is None else "POST",
    )

    try:
        with HTTP.open(http_request, timeout=timeout) as response:
            return response.status, response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8")


def healthy(node, endpoint):
    try:
        status, body = request(node, endpoint)
        if status != 200:
            return None

        result = json.loads(body)
        return result if isinstance(result, dict) else None
    except (
        urllib.error.URLError,
        OSError,
        ValueError,
    ):
        return None


def recover_containers(recovery_started):
    for node in NODES:
        try:
            status = docker(
                "inspect",
                "--format",
                "{{.State.Status}}",
                node,
            )

            if status not in {"exited", "dead"}:
                recovery_started.pop(node, None)
                continue

            peer = next(name for name in NODES if name != node)

            if healthy(peer, "/primary") is None:
                recovery_started.pop(node, None)
                continue

            now = time.monotonic()

            if node not in recovery_started:
                recovery_started[node] = now
                log(
                    f"{node} is down; {peer} is leader. "
                    f"Restarting {node} in {RECOVERY_DELAY_SECONDS} seconds."
                )
                continue

            if now - recovery_started[node] < RECOVERY_DELAY_SECONDS:
                continue

            log(f"Starting {node}; Patroni will recover its database.")
            docker("start", node)
            recovery_started.pop(node, None)

        except (subprocess.SubprocessError, OSError) as error:
            recovery_started.pop(node, None)
            log(f"Container recovery check failed for {node}: {error}")


def failback_ready():
    leader = healthy(OTHER_NODE, "/primary")
    standby = healthy(PREFERRED_LEADER, "/synchronous")

    if leader is None or standby is None:
        return False

    if leader.get("pause") or standby.get("pause"):
        return False

    timeline = leader.get("timeline")
    if timeline is None or timeline != standby.get("timeline"):
        return False

    return any(
        member.get("application_name") == PREFERRED_LEADER
        and member.get("state") == "streaming"
        and member.get("sync_state") == "sync"
        for member in leader.get("replication", [])
    )


def main():
    recovery_started = {}
    stable_since = None
    retry_after = 0
    awaiting_confirmation = False

    log(f"Recovery watcher started. Preferred leader: {PREFERRED_LEADER}.")
    log("Stop this watcher before intentionally shutting down the stack.")

    while True:
        recover_containers(recovery_started)

        preferred_is_primary = (
            healthy(
                PREFERRED_LEADER,
                "/primary",
            )
            is not None
        )

        if preferred_is_primary:
            stable_since = None

            if (
                awaiting_confirmation
                and healthy(OTHER_NODE, "/synchronous") is not None
            ):
                log(
                    f"Failback confirmed: {PREFERRED_LEADER} is leader; "
                    f"{OTHER_NODE} is a synchronous standby."
                )
                awaiting_confirmation = False

        elif not failback_ready():
            if stable_since is not None:
                log("Failback conditions changed; restarting stability timer.")
            stable_since = None

        else:
            now = time.monotonic()

            if stable_since is None:
                stable_since = now
                log(
                    f"{PREFERRED_LEADER} is a healthy synchronous standby. "
                    f"Checking stability for {FAILBACK_STABLE_SECONDS} seconds."
                )

            if now - stable_since >= FAILBACK_STABLE_SECONDS and now >= retry_after:
                log(
                    f"Requesting leadership transfer: "
                    f"{OTHER_NODE} -> {PREFERRED_LEADER}."
                )

                awaiting_confirmation = True

                try:
                    status, body = request(
                        OTHER_NODE,
                        "/switchover",
                        {
                            "leader": OTHER_NODE,
                            "candidate": PREFERRED_LEADER,
                        },
                        timeout=30,
                    )
                    log(f"Switchover response: HTTP {status}: {body.strip()}")
                except (urllib.error.URLError, OSError) as error:
                    log(
                        f"Switchover response unavailable: {error}. "
                        "Will check actual roles before retrying."
                    )

                retry_after = time.monotonic() + SWITCHOVER_RETRY_SECONDS
                stable_since = None

        time.sleep(POLL_INTERVAL_SECONDS)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        log("Recovery watcher stopped.")
