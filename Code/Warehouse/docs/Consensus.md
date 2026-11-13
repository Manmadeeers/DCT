# Consensus and leader election

The lab cluster contains five identical `Warehouse.ConsensusNode` containers.
Every node sends heartbeat requests to its peers and independently selects the
available node with the greatest ID as leader. A proposal is accepted only when
it receives `N / 2 + 1` positive votes, where `N` is the configured cluster
size (three of five nodes in the default setup).

The existing Warehouse project in `src/` is the API gateway. It discovers the
current leader and forwards proposals to it. Swagger is available at
`http://localhost:5000/swagger`.

## Start

```bash
docker --context desktop-linux compose up --build -d
```

Inspect the cluster. Node 5 must be the initial leader:

```bash
curl http://localhost:5000/api/consensus
```

Propose configuration version 15. With all nodes available, consensus is
achieved with five positive votes:

```bash
curl -X POST http://localhost:5000/api/consensus/proposals \
  -H 'Content-Type: application/json' \
  -d '{"configurationVersion":15}'
```

## Simulate failures and votes

Fail node 5. The next cluster status reports node 4 as leader:

```bash
curl -X PUT http://localhost:5000/api/consensus/nodes/5/state \
  -H 'Content-Type: application/json' \
  -d '{"available":false,"acceptVotes":true}'
```

Restore it:

```bash
curl -X PUT http://localhost:5000/api/consensus/nodes/5/state \
  -H 'Content-Type: application/json' \
  -d '{"available":true,"acceptVotes":true}'
```

An available node can be configured to vote `NO` without failing its
heartbeat:

```bash
curl -X PUT http://localhost:5000/api/consensus/nodes/4/state \
  -H 'Content-Type: application/json' \
  -d '{"available":true,"acceptVotes":false}'
```

Fail any three nodes and submit a greater version to observe
`CONSENSUS NOT ACHIEVED` because only two of five votes can be collected.

Stopping a container with
`docker --context desktop-linux compose stop consensus-node-5` models a real
missing heartbeat. Start it again with
`docker --context desktop-linux compose start consensus-node-5`.
