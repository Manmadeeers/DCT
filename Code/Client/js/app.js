(() => {
  'use strict';

  const $ = selector => document.querySelector(selector);

  const state = {
    api: localStorage.getItem('warehouseApiUrl') ||
      'http://localhost:5000',

    ids: null,
    products: [],
    suppliers: [],
    busy: false
  };

  const productSelects = [
    {
      selector: '#receive-product',
      placeholder: 'Choose a product to receive'
    },
    {
      selector: '#reserve-product',
      placeholder: 'Choose a product to reserve'
    },
    {
      selector: '#ship-product',
      placeholder: 'Choose a product to ship'
    }
  ];

  $('#api-url').value = state.api;

  function setApi(value) {
    const url = new URL(value.trim());

    if (!['http:', 'https:'].includes(url.protocol)) {
      throw new Error(
        'Use an http:// or https:// API address.'
      );
    }

    state.api =
      url.origin + url.pathname.replace(/\/+$/, '');

    state.ids = null;
    state.products = [];
    state.suppliers = [];

    localStorage.setItem(
      'warehouseApiUrl',
      state.api
    );
  }

  async function api(path, payload) {
    let response;

    try {
      const options = payload === undefined
        ? {}
        : {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
          };

      response = await fetch(
        state.api + path,
        options
      );
    } catch {
      throw new Error(
        'Cannot reach the API. Check its address, ' +
        'that it is running, and its CORS settings.'
      );
    }

    const text = await response.text();
    let data;

    try {
      data = text
        ? JSON.parse(text)
        : null;
    } catch {
      data = {
        message: text
      };
    }

    if (!response.ok) {
      const error = new Error(
        data?.detail ||
        data?.title ||
        data?.message ||
        `HTTP ${response.status}`
      );

      error.status = response.status;
      error.data = data;

      throw error;
    }

    return {
      status: response.status,
      data
    };
  }

  function responseView(
    label,
    status,
    data,
    failed = false
  ) {
    $('#response-status').textContent =
      `${label} · ${status || 'connection error'}`;

    $('#response-status').classList.toggle(
      'error',
      failed
    );

    $('#response-body').textContent =
      JSON.stringify(data, null, 2);
  }

  let toastTimer;

  function toast(message, failed = false) {
    const element = $('#toast');

    element.textContent = message;

    element.className =
      'toast show' + (failed ? ' error' : '');

    clearTimeout(toastTimer);

    toastTimer = setTimeout(() => {
      element.classList.remove('show');
    }, 4000);
  }

  function log(title, detail, failed = false) {
    const list = $('#activity-list');

    list.querySelector('.empty')?.remove();

    const item =
      document.createElement('li');

    if (failed) {
      item.className = 'error';
    }

    const description =
      document.createElement('span');

    const name =
      document.createElement('strong');

    const note =
      document.createElement('small');

    name.textContent = title;
    note.textContent = detail;

    description.append(name, note);

    const time =
      document.createElement('time');

    time.textContent =
      new Date().toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit'
      });

    item.append(description, time);
    list.prepend(item);

    while (list.children.length > 10) {
      list.lastElementChild.remove();
    }
  }

  function handleError(label, error) {
    responseView(
      label,
      error.status,
      error.data || {
        error: error.message
      },
      true
    );

    log(
      `${label} failed`,
      error.message,
      true
    );

    toast(error.message, true);
  }

  function integer(value) {
    const number = Number(value);

    if (
      !Number.isSafeInteger(number) ||
      number < 1 ||
      number > 2147483647
    ) {
      throw new Error(
        'IDs and quantities must be positive ' +
        'whole numbers within the API range.'
      );
    }

    return number;
  }

  function getProduct(productId) {
    return state.products.find(
      product =>
        Number(product.id) === Number(productId)
    );
  }

  function describeProduct(productId) {
    const product = getProduct(productId);

    if (!product) {
      return `Product #${productId}`;
    }

    return `${product.name} — ${product.sku} ` +
      `(#${product.id})`;
  }

  function getSupplier(supplierId) {
    return state.suppliers.find(
      supplier =>
        Number(supplier.id) === Number(supplierId)
    );
  }

  function describeSupplier(supplierId) {
    const supplier = getSupplier(supplierId);

    if (!supplier) {
      return `Supplier #${supplierId}`;
    }

    return `${supplier.name} (#${supplier.id})`;
  }

  async function bootstrap() {
    const { data } =
      await api('/api/bootstrap');

    state.ids = data;

    $('#warehouse-id').textContent =
      '#' + data.warehouseId;

    $('#stock-id').textContent =
      '#' + data.stockId;

    $('#supplier-id').textContent =
      '#' + data.supplierId;

    return data;
  }

  function populateProductSelect(
    select,
    products,
    selectedProductId,
    placeholderText
  ) {
    select.replaceChildren();

    const placeholder =
      document.createElement('option');

    placeholder.value = '';

    if (products.length === 0) {
      placeholder.textContent =
        'No products available';

      select.append(placeholder);
      select.disabled = true;

      return;
    }

    placeholder.textContent = placeholderText;
    placeholder.disabled = true;
    placeholder.selected = true;

    select.append(placeholder);

    for (const product of products) {
      const option =
        document.createElement('option');

      option.value = String(product.id);

      option.textContent =
        `${product.name} — ${product.sku} ` +
        `(#${product.id})`;

      select.append(option);
    }

    select.disabled = false;

    const exists = products.some(
      product =>
        String(product.id) ===
        String(selectedProductId)
    );

    if (exists) {
      select.value =
        String(selectedProductId);
    }
  }

  async function loadProducts(
    selectedProductId = null
  ) {
    const previousSelections = {};

    for (const config of productSelects) {
      const select = $(config.selector);

      previousSelections[config.selector] =
        selectedProductId ?? select.value;
    }

    const result =
      await api('/api/products');

    state.products =
      Array.isArray(result.data)
        ? result.data
        : [];

    for (const config of productSelects) {
      populateProductSelect(
        $(config.selector),
        state.products,
        previousSelections[config.selector],
        config.placeholder
      );
    }

    return state.products;
  }

  function populateSupplierSelect(
    suppliers,
    selectedSupplierId = null
  ) {
    const select = $('#receive-supplier');

    const previousValue =
      selectedSupplierId ?? select.value;

    select.replaceChildren();

    const placeholder =
      document.createElement('option');

    placeholder.value = '';

    if (suppliers.length === 0) {
      placeholder.textContent =
        'No suppliers available';

      select.append(placeholder);
      select.disabled = true;

      return;
    }

    placeholder.textContent =
      'Choose a supplier';

    placeholder.disabled = true;
    placeholder.selected = true;

    select.append(placeholder);

    for (const supplier of suppliers) {
      const option =
        document.createElement('option');

      option.value = String(supplier.id);

      option.textContent =
        `${supplier.name} (#${supplier.id})`;

      select.append(option);
    }

    select.disabled = false;

    const exists = suppliers.some(
      supplier =>
        String(supplier.id) ===
        String(previousValue)
    );

    if (exists) {
      select.value =
        String(previousValue);
    }
  }

  async function loadSuppliers(
    selectedSupplierId = null
  ) {
    const result =
      await api('/api/suppliers');

    state.suppliers =
      Array.isArray(result.data)
        ? result.data
        : [];

    populateSupplierSelect(
      state.suppliers,
      selectedSupplierId
    );

    return state.suppliers;
  }

  function renderStock(stockData) {
    const items = stockData.items || [];

    $('#stock-name').textContent =
      `${stockData.name} · Stock #${stockData.id}`;

    $('#total').textContent = items
      .reduce(
        (sum, item) => sum + item.quantity,
        0
      )
      .toLocaleString();

    $('#reserved').textContent = items
      .reduce(
        (sum, item) => sum + item.reserved,
        0
      )
      .toLocaleString();

    $('#available').textContent = items
      .reduce(
        (sum, item) => sum + item.available,
        0
      )
      .toLocaleString();

    const body = $('#stock-body');

    body.replaceChildren();

    if (items.length === 0) {
      const cell =
        body.insertRow().insertCell();

      cell.colSpan = 5;
      cell.className = 'empty';

      cell.textContent =
        'No inventory yet. Create a product ' +
        'and receive a shipment.';

      return;
    }

    for (const item of items) {
      const product =
        getProduct(item.productId);

      const row = body.insertRow();

      row.insertCell().textContent =
        `#${item.productId}`;

      const productCell = row.insertCell();

      productCell.className =
        'product-cell';

      const productName =
        document.createElement('strong');

      const productSku =
        document.createElement('small');

      productName.textContent =
        product?.name || 'Unknown product';

      productSku.textContent =
        product?.sku || 'SKU unavailable';

      productCell.append(
        productName,
        productSku
      );

      row.insertCell().textContent =
        item.quantity.toLocaleString();

      row.insertCell().textContent =
        item.reserved.toLocaleString();

      row.insertCell().textContent =
        item.available.toLocaleString();
    }
  }

  async function stock(showResponse = false) {
    const ids =
      state.ids || await bootstrap();

    const result = await api(
      `/api/stocks/${ids.stockId}`
    );

    renderStock(result.data);

    if (showResponse) {
      responseView(
        'GET /api/stocks',
        result.status,
        result.data
      );
    }

    return result.data;
  }

  async function connect() {
    const indicator = $('#health');

    indicator.className = 'health';
    indicator.textContent = '● Checking API';

    try {
      await api('/health');

      indicator.className =
        'health online';

      indicator.textContent =
        '● API online';
    } catch (error) {
      indicator.className =
        'health offline';

      indicator.textContent =
        '● API unavailable';

      handleError(
        'Health check',
        error
      );

      return;
    }

    try {
      await loadProducts();
      await loadSuppliers();

      const ids = await bootstrap();

      populateSupplierSelect(
        state.suppliers,
        ids.supplierId
      );

      await stock(true);
    } catch (error) {
      handleError(
        'Load warehouse data',
        error
      );
    }
  }

  async function operation(name, payload) {
    const result = await api(
      `/api/operations/${name}`,
      payload
    );

    responseView(
      name,
      result.status,
      result.data
    );

    let activityDetail;

    if (name === 'AddProduct') {
      activityDetail =
        `${result.data.name} — ` +
        `${result.data.sku} ` +
        `(#${result.data.id})`;
    } else if (name === 'AddSupplier') {
      activityDetail =
        `${result.data.name} ` +
        `(#${result.data.id})`;
    } else if (name === 'ReceiveShipment') {
      const item = payload.items[0];

      activityDetail =
        `${describeProduct(item.productId)} · ` +
        `${item.quantity} units · ` +
        describeSupplier(payload.supplierId);
    } else {
      activityDetail =
        `${describeProduct(payload.productId)} · ` +
        `${payload.quantity} units`;
    }

    log(name, activityDetail);
    toast(`${name} completed.`);

    if (
      name !== 'AddProduct' &&
      name !== 'AddSupplier'
    ) {
      await stock();
    }

    return result.data;
  }

  async function busy(button, task) {
    if (state.busy) {
      return;
    }

    state.busy = true;

    const originalText =
      button.textContent;

    document
      .querySelectorAll('button')
      .forEach(element => {
        element.disabled = true;
      });

    button.textContent = 'Working…';

    try {
      await task();
    } finally {
      button.textContent = originalText;

      document
        .querySelectorAll('button')
        .forEach(element => {
          element.disabled = false;
        });

      state.busy = false;
    }
  }

  function selectProduct(
    selector,
    productId
  ) {
    const select = $(selector);

    const exists = Array
      .from(select.options)
      .some(
        option =>
          option.value ===
          String(productId)
      );

    if (exists) {
      select.value =
        String(productId);
    }
  }

  function selectSupplier(supplierId) {
    const select =
      $('#receive-supplier');

    const exists = Array
      .from(select.options)
      .some(
        option =>
          option.value ===
          String(supplierId)
      );

    if (exists) {
      select.value =
        String(supplierId);
    }
  }

  function bindForm(
    id,
    operationName,
    buildPayload,
    requiresBootstrap = true
  ) {
    const form = $('#' + id);

    form.addEventListener(
      'submit',
      event => {
        event.preventDefault();

        if (!form.reportValidity()) {
          return;
        }

        busy(
          form.querySelector('button'),
          async () => {
            try {
              const ids = requiresBootstrap
                ? state.ids ||
                  await bootstrap()
                : null;

              const formData =
                new FormData(form);

              const payload =
                buildPayload(
                  formData,
                  ids
                );

              const result =
                await operation(
                  operationName,
                  payload
                );

              form.reset();

              if (
                operationName ===
                'AddProduct'
              ) {
                await loadProducts(
                  result.id
                );
              }

              if (
                operationName ===
                'AddSupplier'
              ) {
                await loadSuppliers(
                  result.id
                );
              }

              if (
                operationName ===
                'ReceiveShipment'
              ) {
                const productId =
                  payload.items[0].productId;

                selectProduct(
                  '#receive-product',
                  productId
                );

                selectProduct(
                  '#reserve-product',
                  productId
                );

                selectProduct(
                  '#ship-product',
                  productId
                );

                selectSupplier(
                  payload.supplierId
                );
              }

              if (
                operationName ===
                'ReserveProduct'
              ) {
                selectProduct(
                  '#reserve-product',
                  payload.productId
                );

                selectProduct(
                  '#ship-product',
                  payload.productId
                );
              }

              if (
                operationName ===
                'ShipProduct'
              ) {
                selectProduct(
                  '#ship-product',
                  payload.productId
                );
              }
            } catch (error) {
              handleError(
                operationName,
                error
              );
            }
          }
        );
      }
    );
  }

  bindForm(
    'add-form',
    'AddProduct',
    data => ({
      sku: data.get('sku').trim(),
      name: data.get('name').trim()
    }),
    false
  );

  bindForm(
    'add-supplier-form',
    'AddSupplier',
    data => ({
      name: data.get('name').trim()
    }),
    false
  );

  bindForm(
    'receive-form',
    'ReceiveShipment',
    (data, ids) => ({
      warehouseId:
        ids.warehouseId,

      stockId:
        ids.stockId,

      supplierId: integer(
        data.get('supplierId')
      ),

      items: [
        {
          productId: integer(
            data.get('productId')
          ),

          quantity: integer(
            data.get('quantity')
          )
        }
      ]
    })
  );

  bindForm(
    'reserve-form',
    'ReserveProduct',
    (data, ids) => ({
      warehouseId:
        ids.warehouseId,

      stockId:
        ids.stockId,

      productId: integer(
        data.get('productId')
      ),

      quantity: integer(
        data.get('quantity')
      )
    })
  );

  bindForm(
    'ship-form',
    'ShipProduct',
    (data, ids) => ({
      warehouseId:
        ids.warehouseId,

      stockId:
        ids.stockId,

      productId: integer(
        data.get('productId')
      ),

      quantity: integer(
        data.get('quantity')
      )
    })
  );

  $('#connection-form').addEventListener(
    'submit',
    event => {
      event.preventDefault();

      busy(
        event.submitter,
        async () => {
          try {
            setApi(
              $('#api-url').value
            );

            await connect();
          } catch (error) {
            handleError(
              'Connect',
              error
            );
          }
        }
      );
    }
  );

  $('#refresh').addEventListener(
    'click',
    event => {
      busy(
        event.currentTarget,
        async () => {
          try {
            await loadProducts();
            await loadSuppliers();
            await stock(true);

            toast(
              'Products, suppliers, and ' +
              'inventory refreshed.'
            );
          } catch (error) {
            handleError(
              'Refresh',
              error
            );
          }
        }
      );
    }
  );

  $('#clear').addEventListener(
    'click',
    () => {
      const list =
        $('#activity-list');

      list.replaceChildren();

      const item =
        document.createElement('li');

      item.className = 'empty';

      item.textContent =
        'Operations will appear here.';

      list.append(item);
    }
  );

  $('#demo').addEventListener(
    'click',
    event => {
      busy(
        event.currentTarget,
        async () => {
          try {
            const ids =
              state.ids ||
              await bootstrap();

            const sku =
              `DEMO-${Date.now()
                .toString(36)
                .toUpperCase()}` +
              `-${Math.random()
                .toString(36)
                .slice(2, 6)
                .toUpperCase()}`;

            const product =
              await operation(
                'AddProduct',
                {
                  sku,
                  name: 'Demo Product'
                }
              );

            await loadProducts(
              product.id
            );

            const supplierId =
              ids.supplierId;

            const common = {
              warehouseId:
                ids.warehouseId,

              stockId:
                ids.stockId,

              productId:
                product.id
            };

            await operation(
              'ReceiveShipment',
              {
                warehouseId:
                  ids.warehouseId,

                stockId:
                  ids.stockId,

                supplierId,

                items: [
                  {
                    productId:
                      product.id,

                    quantity: 10
                  }
                ]
              }
            );

            await operation(
              'ReserveProduct',
              {
                ...common,
                quantity: 3
              }
            );

            await operation(
              'ShipProduct',
              {
                ...common,
                quantity: 3
              }
            );

            selectProduct(
              '#receive-product',
              product.id
            );

            selectProduct(
              '#reserve-product',
              product.id
            );

            selectProduct(
              '#ship-product',
              product.id
            );

            selectSupplier(
              supplierId
            );

            log(
              'Demo complete',
              `${describeProduct(product.id)}: ` +
              '7 on hand, 0 reserved, ' +
              '7 available.'
            );

            toast(
              `Demo complete · ` +
              `${product.name} has ` +
              '7 units available.'
            );

            $('.inventory')
              .scrollIntoView({
                behavior: 'smooth',
                block: 'center'
              });
          } catch (error) {
            handleError(
              'Demo scenario',
              error
            );
          }
        }
      );
    }
  );

  connect();
})();