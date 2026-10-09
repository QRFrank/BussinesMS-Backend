# BussinesMS - Entity Relationship Diagram

> Este documento define el modelo de datos completo del sistema.
> Se usa como referencia para crear nuevos módulos.
> Última actualización: 22/09/2026 — Auditoría completa contra el código real: Compras, Inventario (con `InventarioLoteAlmacen`) y Ventas/Caja quedaron documentados como implementados (antes figuraban como pendientes).

---

## DB_AUTH — Base de datos de autenticación

**Propósito**: Usuarios, Roles, Almacenes. Compartida por DB_Regular y DB_Navidad. Los IDs de esta DB viajan en el JWT token.

### Tablas

| Tabla | Descripción |
|-------|-------------|
| Sistema | 1: Regular, 2: Navideño |
| Rol | Admin, VendedorTienda, VendedorRuta, EncargadoAlmacen, Contador |
| Usuario | Usuarios del sistema con JWT |
| Almacen | Almacenes por sistema (`SistemaId`, default 1 = Regular). Seed: Tienda Principal, Almacén 1, Almacén 2 |
| UsuarioSistema | A qué sistemas puede entrar cada usuario (login multisistema) |

### Esquema

```
Table Sistema {
  Id int [pk, increment]
  Nombre nvarchar(50) [not null, note: '1: Regular, 2: Navideño']
  IsActive bit [not null, default: true]
}

Table Rol {
  Id int [pk, increment]
  Nombre nvarchar(50) [not null, unique,
    note: 'Admin, VendedorTienda, VendedorRuta, EncargadoAlmacen, Contador']
  Permisos nvarchar(max) [null, note: 'JSON array de permisos: ["venta.crear","caja.abrir"]']
  IsActive bit [not null, default: true]
}

Table Usuario {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null]
  Apellido nvarchar(100) [not null]
  Email nvarchar(150) [null]
  Username nvarchar(50) [not null, unique]
  PasswordHash nvarchar(255) [not null]
  RolId int [not null]
  SistemaIdDefault int [not null, default: 1]
  IsActive bit [not null, default: true]
}

Table Almacen {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null]
  Codigo nvarchar(20) [not null, unique]
  EsTienda bit [not null, default: false,
    note: 'true = Tienda Principal, false = Deposito']
  Direccion nvarchar(255) [null]
  SistemaId int [not null, default: 1, note: 'FK a Sistema. GET /Almacenes filtra por query param sistemaId → claim sistemaId → 1']
  IsActive bit [not null, default: true]
}

Table UsuarioSistema {
  UsuarioId int [pk, note: 'FK a Usuario (cascade)']
  SistemaId int [pk, note: 'FK a Sistema. Backfill: una fila por usuario con su SistemaIdDefault']
}

Ref: Usuario.RolId > Rol.Id
Ref: Almacen.SistemaId > Sistema.Id
Ref: UsuarioSistema.UsuarioId > Usuario.Id
Ref: UsuarioSistema.SistemaId > Sistema.Id
```

---

## DB_SISTEMA — Sistema principal (todo el año)

**Propósito**: Catálogo de productos, inventario, compras, ventas y caja.

> **Nota importante**: Los campos UsuarioId y AlmacenId son int simples. NO tienen FK hacia DB_Auth (vienen del JWT).

---

### MÓDULO 1: CATÁLOGO DE PRODUCTOS

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Categoria | ✅ | Categorías de productos |
| Fabricante | ✅ | Fabricantes de productos |
| DescripcionSabor | ✅ | Sabores disponibles |
| DescripcionTamanio | ✅ | Tamaños (100g, 500ml, 1kg) |
| TipoPresentacion | ✅ | Unidad, Caja, Caja2 |
| Producto | ✅ | Producto maestro |
| ProductoVariante | ✅ | Variante (sabor + tamaño) |
| ProductoPresentacion | ✅ | Equivalencias por presentación (jerárquico) |
| HistorialPrecio | ⏳ | Histórico de cambios de precio |

#### Esquema

```
Table Categoria {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null]
  Descripcion nvarchar(255) [null]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table Fabricante {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null]
  Descripcion nvarchar(255) [null]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table DescripcionSabor {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null, unique]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table DescripcionTamanio {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null, unique, note: 'Ej: 100g, 500ml, 1kg']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table TipoPresentacion {
  Id int [pk, increment]
  Nombre nvarchar(50) [not null, unique, note: 'Unidad, Caja, Caja2']
  Orden int [not null, note: 'Para mostrar de menor a mayor: 1=Unidad, 2=Caja, 3=Caja2']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table Producto {
  Id int [pk, increment]
  CodigoInterno nvarchar(20) [null, unique, note: 'Generado: PROD-00001']
  Nombre nvarchar(150) [not null]
  CategoriaId int [not null]
  FabricanteId int [null, note: 'Opcional']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}

Table ProductoVariante {
  Id int [pk, increment]
  ProductoId int [not null]
  NombreProducto nvarchar(150) [null, note: 'Copia del nombre del producto al momento de crear la variante']
  CodigoBarras nvarchar(50) [null]
  SaborId int [not null]
  SaborDescripcion nvarchar(100) [null, note: 'Copia del nombre del sabor']
  TamanioId int [not null]
  PesoTamanio nvarchar(50) [null, note: 'Copia del nombre del tamaño']
  PrecioVentaActual decimal(18,2) [not null, default: 0,
    note: 'Cache del precio vigente. La historia está en HistorialPrecio']
  PrecioCompra decimal(18,2) [not null, default: 0,
    note: 'Último precio de compra registrado']
  CodigoAlmacen nvarchar(20) [null, note: 'Código del almacén al que pertenece']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]

  indexes {
    (ProductoId, SaborId, TamanioId) [unique, name: 'UQ_Variante_Combinacion']
  }
}

Table ProductoPresentacion {
  Id int [pk, increment]
  VarianteId int [not null]
  TipoPresentacionId int [not null]
  NombrePersonalizado nvarchar(50) [null,
    note: 'Nombre custom: "Display","Tira","Botella". null = usa nombre del TipoPresentacion']
  CantidadDePadre int [not null, default: 1,
    note: 'Cuántas unidades del padre contiene. Unidad=1, Caja=12, etc.']
  PresentacionPadreId int [null,
    note: 'null = raíz (Unidad). FK self-ref a la presentación padre']
  EsDefaultReporte bit [not null, default: false,
    note: 'Solo 1 true por VarianteId. Presentación usada en reportes']
  CodigoBarras nvarchar(50) [null, unique,
    note: 'Código de barras específico de esta presentación']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]

  indexes {
    (VarianteId, TipoPresentacionId) [unique, name: 'UQ_Presentacion_Variante']
    (VarianteId) [unique, where: 'EsDefaultReporte = 1', name: 'UQ_Presentacion_DefaultReporte']
  }
}

Table HistorialPrecio {
  Id int [pk, increment]
  VarianteId int [not null]
  PrecioAnterior decimal(18,2) [not null]
  PrecioNuevo decimal(18,2) [not null]
  FechaCambio datetime2 [not null, default: `GETDATE()`]
  MotivoCambio nvarchar(255) [null]
  CambiadoByUsuarioId int [not null, note: 'ID del JWT, sin FK']
}

Ref: Producto.CategoriaId > Categoria.Id
Ref: Producto.FabricanteId > Fabricante.Id
Ref: ProductoVariante.ProductoId > Producto.Id
Ref: ProductoVariante.SaborId > DescripcionSabor.Id
Ref: ProductoVariante.TamanioId > DescripcionTamanio.Id
Ref: ProductoPresentacion.VarianteId > ProductoVariante.Id
Ref: ProductoPresentacion.TipoPresentacionId > TipoPresentacion.Id
Ref: ProductoPresentacion.PresentacionPadreId > ProductoPresentacion.Id
Ref: HistorialPrecio.VarianteId > ProductoVariante.Id
```

---

### MÓDULO 2: PROVEEDORES

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Proveedor | ✅ | Proveedores del sistema |

#### Esquema

```
Table Proveedor {
  Id int [pk, increment]
  Nombre nvarchar(150) [not null]
  Nit nvarchar(20) [null]
  Telefono nvarchar(20) [null]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
  DeletedAt datetime2 [null]
  CreatedByUsuarioId int [not null, note: 'ID del JWT, sin FK']
  UpdatedByUsuarioId int [null]
  DeletedByUsuarioId int [null]
}
```

---

### MÓDULO 3: INVENTARIO Y LOTES

> **Refactor (commit `692f6d8`)**: el stock por almacén se separó del lote. `InventarioLote` ahora es el "lote lógico" (costo, vencimiento, estado), y `InventarioLoteAlmacen` guarda cuánto stock de ese lote hay en cada almacén. `Traslado` **no es una tabla propia** — se implementa como un `MovimientoInventario` con `TipoMovimiento = Traslado` (ver `TrasladoService`/`TrasladosController`).

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| InventarioLote | ✅ | Lote lógico: costo, vencimiento, estado (sin stock por almacén) |
| InventarioLoteAlmacen | ✅ | Stock disponible de un lote, por almacén (no documentada anteriormente) |
| MovimientoInventario | ✅ | Auditoría de movimientos de stock (apunta a InventarioLoteAlmacen) |
| Traslado | ✅ (no es tabla) | Se registra como MovimientoInventario con TipoMovimiento=Traslado |

#### Esquema

```
Table InventarioLote {
  Id int [pk, increment]
  VarianteId int [not null]
  CompraDetalleId int [null, note: 'null = lote nacido de traslado parcial. Si viene de compra, referencia el detalle (de ahí se obtiene ProveedorId vía join)']
  CodigoLote nvarchar(60) [null, note: 'Agregado en migración AddCodigoLoteToInventarioLote']

  StockInicial int [not null, note: 'Cantidad recibida originalmente, NUNCA cambia']
  CantidadVendida int [not null, default: 0]
  CantidadVencida int [not null, default: 0, note: 'Lo que venció sin venderse']

  CostoCompraUnitario decimal(18,4) [not null]

  FechaVencimiento date [null, note: 'null = no vence']
  EstadoLote int [not null, default: 1, note: '1:Activo, 2:Agotado, 3:Vencido']

  IsActive bit [not null, default: true]
  CreatedAt datetime2
  UpdatedAt datetime2
  CreatedByUsuarioId int

  note: 'Ya NO tiene AlmacenId ni StockDisponible directos — eso vive en InventarioLoteAlmacen. Precio de venta tampoco vive acá, está en ProductoVariante (PrecioVentaUnitario/PrecioVentaMayoreo).'
}

// Stock disponible de un lote, desglosado por almacén
Table InventarioLoteAlmacen {
  Id int [pk, increment]
  LoteId int [not null]
  VarianteId int [not null]
  AlmacenId int [not null]
  StockDisponible int [not null, note: 'Disminuye al vender, trasladar (parcial) o marcar vencido']

  indexes {
    (VarianteId, AlmacenId, FechaVencimiento) [name: 'IX_LoteAlmacen_FEFO']
    (VarianteId, AlmacenId) [name: 'IX_LoteAlmacen_VarianteAlmacen']
    (LoteId, AlmacenId) [unique, name: 'UQ_LoteAlmacen']
  }
}

// Auditoría inmutable de cada cambio de stock
Table MovimientoInventario {
  Id int [pk, increment]
  LoteAlmacenId int [not null, note: 'FK a InventarioLoteAlmacen, no a InventarioLote directamente']
  VarianteId int [not null]
  AlmacenOrigenId int [null, note: 'null si es entrada pura (compra)']
  AlmacenDestinoId int [null, note: 'null si es salida pura (venta)']
  TipoMovimiento int [not null,
    note: '1:EntradaCompra, 2:SalidaVenta, 3:Traslado, 4:AjustePositivo, 5:AjusteNegativo, 6:Vencimiento. Traslado usa este tipo, no existe tabla Traslado separada']
  CantidadUnidades int [not null, note: 'Siempre positivo. El tipo indica si suma o resta']
  SaldoResultante int [not null, note: 'StockDisponible del lote-almacén DESPUÉS de este movimiento']
  ReferenciaId int [null,
    note: 'ID del documento origen: VentaDetalleId, CompraDetalleId, etc.']
  Observacion nvarchar(255) [null]
  FechaMovimiento datetime2 [not null, default: `GETDATE()`]
  UsuarioId int [not null, note: 'ID del JWT, sin FK']
}

Ref: InventarioLote.VarianteId > ProductoVariante.Id
Ref: InventarioLote.CompraDetalleId > CompraDetalle.Id
Ref: InventarioLoteAlmacen.LoteId > InventarioLote.Id
Ref: InventarioLoteAlmacen.VarianteId > ProductoVariante.Id
Ref: MovimientoInventario.LoteAlmacenId > InventarioLoteAlmacen.Id
Ref: MovimientoInventario.VarianteId > ProductoVariante.Id
```

`DevolucionCliente` (devolución de venta) también referencia `InventarioLoteAlmacen` (`LoteAlmacenOrigenId`, `LoteAlmacenDevueltoId`), no `InventarioLote` directamente.

---

### MÓDULO 4: COMPRAS

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Compra | ✅ | Orden de compra |
| CompraDetalle | ✅ | Detalle de productos comprados |
| PagoCompra | ✅ | Pagos parciales a proveedores |

#### Esquema

```
Table Compra {
  Id int [pk, increment]
  ProveedorId int [not null]
  UsuarioId int [not null, note: 'ID del JWT, sin FK']
  AlmacenId int [not null, note: 'Almacén destino principal, ID del JWT, sin FK']
  FechaCompra datetime2 [not null, default: `GETDATE()`]
  TotalCompra decimal(18,2) [not null]
  EstadoPago int [not null, default: 1,
    note: '1:Pagado, 2:Credito, 3:ParcialmentePagado']
  NumeroFactura nvarchar [null]
  EstaLiquidada bit [not null, default: false]
  Observacion nvarchar(500) [null]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
}

// Detalle "ligero" (no hereda EntidadBase). CantidadUnidades es el TOTAL consolidado
// de la variante en la compra (ya en unidades base) — la distribución por almacén
// no vive acá, se resuelve creando N filas InventarioLote/InventarioLoteAlmacen
// (con CompraDetalleId apuntando a este registro) al confirmar la compra.
Table CompraDetalle {
  Id int [pk, increment]
  CompraId int [not null]
  VarianteId int [not null]
  CantidadUnidades int [not null, note: 'Total en unidades base, ya convertido en el frontend']
  CostoUnitario decimal(18,4) [not null]
  Subtotal decimal(18,2) [not null]
  FechaVencimiento date [null]
}

Table PagoCompra {
  Id int [pk, increment]
  CompraId int [not null]
  Monto decimal(18,2) [not null]
  FechaPago datetime2 [not null, default: `GETDATE()`]
  SesionCajaId int [null,
    note: 'Si el pago salió de una caja activa, se registra aquí']
  Observacion nvarchar(255) [null]
  RegistradoByUsuarioId int [not null, note: 'ID del JWT, sin FK']
}

Ref: Compra.ProveedorId > Proveedor.Id
Ref: CompraDetalle.CompraId > Compra.Id
Ref: CompraDetalle.VarianteId > ProductoVariante.Id
Ref: PagoCompra.CompraId > Compra.Id
Ref: PagoCompra.SesionCajaId > SesionCaja.Id
```

---

### MÓDULO 5: VENTAS Y CAJA

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| SesionCaja | ✅ | Sesión de caja por usuario |
| Venta | 🟡 | Venta en tienda — funcional, endpoint de productos del POS pendiente de confirmar (ver nota abajo) |
| VentaDetalle | ✅ | Detalle con lote específico (FIFO) |
| CategoriaGasto | ✅ | Categorías de gasto |
| GastoOperativo | ✅ | Gastos operativos y pagos a proveedores |

> **Estado real (2026-09-22)**: entidad + repositorio + servicio + controller + migración están implementados (`VentasController`, `SesionesCajaController`, migración `AgregarVentasYCaja`). **Pendiente**: el endpoint de productos que consume el POS de venta todavía no está confirmado/ajustado — debe devolver solo variantes con stock disponible y excluir/filtrar lotes vencidos, sin romper el endpoint genérico que usa el módulo de Inventario. `ProductoVariantesController` ya expone `GET /stock-pos?almacenId=`, que parece pensado para esto — falta confirmar que cubre el caso y conectarlo bien desde el frontend.

#### Esquema

```
Table SesionCaja {
  Id int [pk, increment]
  UsuarioId int [not null, note: 'ID del JWT, sin FK']
  AlmacenId int [not null, note: 'ID del JWT/DB_Auth, sin FK']
  FechaApertura datetime2 [not null, default: `GETDATE()`]
  FechaCierre datetime2 [null]
  MontoInicial decimal(18,2) [not null, default: 0]
  IngresosEfectivo decimal(18,2) [not null, default: 0,
    note: 'Suma de ventas en efectivo de la sesión']
  IngresosDigitales decimal(18,2) [not null, default: 0,
    note: 'Suma de ventas por QR/transferencia']
  EgresosGastos decimal(18,2) [not null, default: 0,
    note: 'Suma de GastoOperativo.Monto de la sesión']
  EgresosPagoProveedor decimal(18,2) [not null, default: 0,
    note: 'Suma de PagoCompra de la sesión. Resta del efectivo esperado']
  MontoEsperadoEfectivo decimal(18,2) [null,
    note: 'Calculado al cierre: MontoInicial + IngresosEfectivo - EgresosGastos - EgresosPagoProveedor']
  MontoRealEntregado decimal(18,2) [null,
    note: 'Lo que el vendedor entregó físicamente']
  Diferencia decimal(18,2) [null,
    note: 'MontoRealEntregado - MontoEsperadoEfectivo']
  Estado int [not null, default: 1,
    note: '1:Abierta, 2:Cerrada, 3:Ajustada']
  CreatedAt datetime2 [not null, default: `GETDATE()`]
  UpdatedAt datetime2 [null]
}

Table Venta {
  Id int [pk, increment]
  UsuarioId int [not null, note: 'ID del JWT, sin FK']
  AlmacenId int [not null, note: 'ID del JWT/DB_Auth, sin FK']
  SesionCajaId int [not null]
  FechaVenta datetime2 [not null, default: `GETDATE()`]
  TotalBruto decimal(18,2) [not null]
  DescuentoTotal decimal(18,2) [not null, default: 0]
  TotalNeto decimal(18,2) [not null,
    note: 'TotalBruto - DescuentoTotal. Campo calculado, no editable']
  MetodoPago int [not null,
    note: '1:Efectivo, 2:TransferenciaQR, 3:Mixto']
  MotivoDescuento nvarchar(255) [null,
    note: 'Obligatorio si DescuentoTotal > 0']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
}

Table VentaDetalle {
  Id int [pk, increment]
  VentaId int [not null]
  VarianteId int [not null]
  LoteId int [not null,
    note: 'Lote específico del que salió esta unidad (FIFO)']
  CantidadUnidades int [not null]
  PrecioUnitarioCobrado decimal(18,2) [not null,
    note: 'Precio vigente al momento de la venta. Histórico inmutable']
  CostoUnitarioLote decimal(18,4) [not null,
    note: 'Costo del lote al momento de la venta. Para calcular margen']
  Subtotal decimal(18,2) [not null,
    note: 'CantidadUnidades * PrecioUnitarioCobrado. Campo calculado']
}

Table CategoriaGasto {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null, unique,
    note: 'Luz, Agua, Alquiler, Sueldo, Combustible, Repuesto, Otro']
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null, default: `GETDATE()`]
}

Table GastoOperativo {
  Id int [pk, increment]
  SesionCajaId int [not null]
  CategoriaGastoId int [not null]
  Monto decimal(18,2) [not null]
  Descripcion nvarchar(500) [null]
  EsPagoProveedor bit [not null, default: false,
    note: 'true = este gasto es un pago a proveedor que sale de la caja']
  PagoCompraId int [null,
    note: 'FK a PagoCompra si EsPagoProveedor = true']
  FechaGasto datetime2 [not null, default: `GETDATE()`]
  RegistradoByUsuarioId int [not null, note: 'ID del JWT, sin FK']
}

Ref: Venta.SesionCajaId > SesionCaja.Id
Ref: VentaDetalle.VentaId > Venta.Id
Ref: VentaDetalle.VarianteId > ProductoVariante.Id
Ref: VentaDetalle.LoteId > InventarioLote.Id
Ref: GastoOperativo.SesionCajaId > SesionCaja.Id
Ref: GastoOperativo.CategoriaGastoId > CategoriaGasto.Id
Ref: GastoOperativo.PagoCompraId > PagoCompra.Id
```

---

## DB_NAVIDAD — Sistema Navideño (temporada de fin de año)

**Propósito**: venta de panetones de fin de año (`SistemaId = 2` en AuthDB). BD `BussinesMS_Navidad`, conexión `NavidadDB`, contexto `NavidadDbContext`, migraciones en `src/BussinesMS.Infraestructura/Migrations/NavidadDb/` (factory de diseño `NavidadDbContextFactory`). Spec: `../docs/navidad/SPEC-navidad.md` (raíz del proyecto).

**Reglas comunes**:
- Todas las tablas heredan `EntidadBase` (`Id`, `IsActive`, `CreatedAt` UTC, `UpdatedAt`, `DeletedAt`, `CreatedByUsuarioId`, `UpdatedByUsuarioId`, `DeletedByUsuarioId`). Borrado siempre lógico.
- Todo dato de temporada lleva `TemporadaId` y el backend trabaja siempre sobre la **temporada abierta** (`ITemporadaActualService`). Una temporada cerrada es de solo lectura.
- Fechas de negocio (`Fecha`, `FechaInicio`, `FechaCierre`) son columnas `date`: se guardan tal cual las manda el cliente (fecha local Bolivia), sin conversión de zona horaria.
- Las referencias a AuthDB (`TemporadaAlmacenConteo.AlmacenId`, `CreatedByUsuarioId`) van sin FK física.

### MÓDULO temporada-capital

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Temporada | ✅ | Temporada anual. Solo una abierta (índice único filtrado `UX_Temporada_UnaAbierta`) |
| Inversor | ✅ | Inversores (global, no por temporada) |
| AporteCapital | ✅ | Capital aportado en la temporada (propio o de inversor) con % de comisión |
| PagoInversor | ✅ | Pagos a inversores: comisión o devolución de capital |
| CategoriaGastoNav | ✅ | Categorías de gasto navideñas (global) |
| GastoNav | ✅ | Gastos de la temporada |
| TemporadaAlmacenConteo | ✅ | Tiendas navideñas con apertura y cierre diario obligatorios en la temporada (0..N, opción B 2026-10-01) |

> Migraciones: `20261001163014_InicialTemporadaCapital` (crea la BD y las 6 tablas) y `20261001191956_TemporadaAlmacenConteo` (crea `TemporadaAlmacenConteo`, migra la antigua `TiendaPrincipalAlmacenId` como una fila y borra esa columna).

#### Esquema

```
Table Temporada {
  Id int [pk, increment]
  Anio int [not null]
  Nombre nvarchar(100) [not null]
  FechaInicio date [not null]
  FechaCierre date [null, note: 'Se completa al cerrar']
  Estado int [not null, note: 'EstadoTemporada: 1 Abierta, 2 Cerrada. Índice único filtrado [Estado] = 1']
}

Table TemporadaAlmacenConteo {
  TemporadaId int [pk, note: 'FK a Temporada (cascade)']
  AlmacenId int [pk, note: 'Almacen de AuthDB con SistemaId=2, EsTienda=true y activo, sin FK']
  CreatedAt datetime2 [not null]
  CreatedByUsuarioId int [not null]
  Note: 'Tabla puente sin EntidadBase. Tiendas que requieren apertura y cierre diario en la temporada'
}

Table Inversor {
  Id int [pk, increment]
  Nombre nvarchar(150) [not null]
  Documento nvarchar(50) [null]
  Telefono nvarchar(50) [null]
}

Table AporteCapital {
  Id int [pk, increment]
  TemporadaId int [not null]
  InversorId int [null, note: 'null = capital propio']
  Monto decimal(18,2) [not null]
  Fecha date [not null]
  PorcentajeComision decimal(5,2) [not null, note: '0 para capital propio. Comisión = Monto × % / 100']
  Observacion nvarchar(500) [null]
}

Table PagoInversor {
  Id int [pk, increment]
  TemporadaId int [not null, note: 'Copiado del aporte']
  AporteCapitalId int [not null]
  Monto decimal(18,2) [not null]
  Fecha date [not null]
  Tipo int [not null, note: 'TipoPagoInversor: 1 Comision, 2 DevolucionCapital']
  Observacion nvarchar(500) [null]
}

Table CategoriaGastoNav {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null, unique]
}

Table GastoNav {
  Id int [pk, increment]
  TemporadaId int [not null]
  CategoriaId int [not null]
  Fecha date [not null]
  Descripcion nvarchar(500) [not null]
  Monto decimal(18,2) [not null]
}

Ref: AporteCapital.TemporadaId > Temporada.Id
Ref: AporteCapital.InversorId > Inversor.Id
Ref: PagoInversor.TemporadaId > Temporada.Id
Ref: PagoInversor.AporteCapitalId > AporteCapital.Id
Ref: GastoNav.TemporadaId > Temporada.Id
Ref: GastoNav.CategoriaId > CategoriaGastoNav.Id
Ref: TemporadaAlmacenConteo.TemporadaId > Temporada.Id
```

### MÓDULO catalogo

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Proveedor | ✅ | Proveedores de la temporada |
| CodigoCliente | ✅ | Códigos de cliente de un proveedor con `UsaCodigosCliente` |
| Producto | ✅ | Productos de la temporada, por proveedor: descripción del proveedor, alias opcional y empaque (java/caja) opcional |
| ~~ProductoPresentacion~~ | ❌ eliminada | Se borró en `ProductoAliasEmpaque` (2026-10-08); la reemplaza el empaque en `Producto` |
| Vendedor | ✅ | Vendedores de la temporada (usuario de AuthDB) |
| ClienteNav | ✅ | Clientes navideños (global) |
| CategoriaProductoNav | ✅ | Categorías de producto (global): Panetones, Galletas… |

> Migraciones:
> - `20261001233759_Catalogo`: crea las 6 tablas, sin tocar las existentes.
> - `20261002010339_CatalogoAjuste1`:
>   - crea `CategoriaProductoNav` y carga Panetones (1) y Galletas (2);
>   - renombra `ProductoPresentacion.PrecioVenta` a `PrecioUnitario`, conservando los valores;
>   - agrega `Producto.PrecioCatalogo` (se inicializa con `PrecioCompraUnidad`) y `Producto.CategoriaProductoId` (los productos existentes quedan en Panetones);
>   - borra `Producto.ComisionRutaPorUnidad`, `Vendedor.TipoComision` y `Vendedor.PorcentajeComision`.
> - `20261008141622_ProductoAliasEmpaque` (Ajustes 2 y 3):
>   - renombra `Producto.Nombre` a `Descripcion`, conservando los valores; el índice pasa a `UX_Producto_Proveedor_Descripcion`;
>   - agrega `Producto.Nombre` (alias, nullable, con el índice único filtrado `UX_Producto_Temporada_Nombre`);
>   - agrega `UnidadesPorEmpaque` y `NombreEmpaque`. El paso de datos copia, de cada producto, la presentación activa con `Unidades > 1`: la principal, o si no hay principal, la de más unidades;
>   - **borra la tabla `ProductoPresentacion`**.
> - El sistema no calcula comisiones de vendedores: solo registrará el pago (módulo `pagos-vendedores`).
> - **Regla de visualización:** en todo el sistema un producto se muestra por `Nombre ?? Descripcion`. Los DTOs lo exponen como `nombreMostrar` (en Producto) o `productoNombreMostrar` (en cualquier otro DTO que incluya un producto).
> Unicidad con índices únicos **filtrados por `[IsActive] = 1`**, para que se pueda recrear un registro después de un borrado lógico. Para hacer DML a mano con sqlcmd hace falta `-I`.
> El catálogo de una temporada anterior se copia a la abierta con `POST api/Navidad/Temporadas/{destinoId}/copiar-catalogo`: copia proveedores, códigos, productos (con alias y empaque) y vendedores válidos.

#### Esquema

```
Table Proveedor {
  Id int [pk, increment]
  TemporadaId int [not null]
  Nombre nvarchar(150) [not null, note: 'Único por temporada entre activos (UX_Proveedor_Temporada_Nombre)']
  UsaCodigosCliente bit [not null, note: 'No se puede apagar si tiene códigos activos']
  TrabajaConPedido bit [not null, default: 0, note: 'Si es 1 (SOALPRO y CARSA): tiene pedidos (uno por código) y recepciones; la deuda nace del pedido. Compras: cualquier proveedor. (PreciosFijos se borró en el Ajuste 2)']
  Telefono nvarchar(50) [null]
  Observacion nvarchar(500) [null]
}

Table CodigoCliente {
  Id int [pk, increment]
  TemporadaId int [not null, note: 'Copiado del proveedor']
  ProveedorId int [not null, note: 'Solo proveedores con UsaCodigosCliente = 1']
  Codigo nvarchar(50) [not null, note: 'Único por proveedor entre activos (UX_CodigoCliente_Proveedor_Codigo)']
  Titular nvarchar(150) [not null]
}

Table Producto {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProveedorId int [not null]
  Descripcion nvarchar(150) [not null, note: 'Nombre en el catálogo del proveedor. Único por proveedor entre activos (UX_Producto_Proveedor_Descripcion)']
  Nombre nvarchar(150) [null, note: 'Alias. Vacío = null. Único por temporada entre activos si viene (UX_Producto_Temporada_Nombre). Se muestra Nombre ?? Descripcion']
  CategoriaProductoId int [not null, note: 'FK a CategoriaProductoNav (global)']
  PrecioCompraUnidad decimal(18,2) [not null, note: '>= 0 (0 = sin precio). Sugerencia; cada lote (RecepcionDetalle) guarda su precio real. 0 al copiar catálogo']
  PrecioCatalogo decimal(18,2) [not null, note: '>= 0 (0 = sin precio). Único precio de referencia (mayorista por unidad, no bloquea). 0 al copiar catálogo']
  UnidadesPorEmpaque int [null, note: '> 1. Unidades por java/caja. Null = solo por unidad. Va junto con NombreEmpaque']
  NombreEmpaque nvarchar(20) [null, note: 'Java / Caja (texto libre). Obligatorio si hay UnidadesPorEmpaque, null si no. Sin precio propio']
}

Table CategoriaProductoNav {
  Id int [pk, increment]
  Nombre nvarchar(100) [not null, unique, note: 'Único también entre inactivas (igual que CategoriaGastoNav)']
}

Table Vendedor {
  Id int [pk, increment]
  TemporadaId int [not null]
  UsuarioId int [not null, note: 'Usuario de AuthDB, sin FK. Único por temporada entre activos (UX_Vendedor_Temporada_Usuario)']
  Tipo int [not null, note: 'TipoVendedor: 1 Tienda, 2 Ruta']
  SueldoMensual decimal(18,2) [null, note: 'Obligatorio > 0 en Tienda; null en Ruta. Sin campos de comisión: el sistema no las calcula']
}

Table ClienteNav {
  Id int [pk, increment]
  Nombre nvarchar(150) [not null]
  Documento nvarchar(50) [null, note: 'Único entre activos si viene (UX_ClienteNav_Documento)']
  Telefono nvarchar(50) [null]
  Direccion nvarchar(250) [null]
}

Ref: Proveedor.TemporadaId > Temporada.Id
Ref: CodigoCliente.TemporadaId > Temporada.Id
Ref: CodigoCliente.ProveedorId > Proveedor.Id
Ref: Producto.TemporadaId > Temporada.Id
Ref: Producto.ProveedorId > Proveedor.Id
Ref: Producto.CategoriaProductoId > CategoriaProductoNav.Id
Ref: Vendedor.TemporadaId > Temporada.Id
```

### MÓDULO abastecimiento

#### Tablas

| Tabla | Estado | Descripción |
|-------|--------|-------------|
| Pedido | ✅ | Solo proveedores con `TrabajaConPedido` (Ajuste 2). Uno por código (o por proveedor sin códigos) por temporada (`UX_Pedido_Proveedor_Codigo`). `MontoTotalProveedor` = origen de la deuda. Sin borrado: solo se edita. No modifica el producto |
| PedidoDetalle | ✅ | Producto, unidades (>= 0) y precio de compra de la nota del código. El PUT reemplaza los detalles sin bajar de lo recibido |
| Recepcion | ✅ | Llegada de un pedido (solo proveedores con `TrabajaConPedido`). No genera deuda. No se edita: se anula si el stock actual de cada almacén alcanza |
| RecepcionDetalle | ✅ | Entrada: producto, unidades y precio (de la línea del pedido). Ya no es lote |
| RecepcionDistribucion | ✅ | Reparto de la línea por almacén navideño |
| Compra | ✅ | (Ajuste 2) Compra a cualquier proveedor, sin código; al contado (`PagoProveedorId` = pago automático) o a crédito. Origen de deuda. Se anula si el stock alcanza |
| CompraDetalle | ✅ | (Ajuste 2) Cualquier producto activo de la temporada, unidades y precio de compra (> 0) |
| CompraDistribucion | ✅ | (Ajuste 2) Reparto de la línea por almacén navideño |
| StockAlmacen | ✅ | (Ajuste 2, reemplaza a `LoteAlmacen`) Stock por producto × almacén, saldo materializado con check >= 0 |
| MovimientoNav | ✅ | Kardex navideño (sin `LoteId` desde el Ajuste 2): cada cambio de `StockAlmacen` va con su movimiento en la misma transacción |
| PagoProveedor | ✅ | Pagos a proveedor o código. Anulación lógica (el pago automático de una compra se anula anulando la compra) |

> Migración `20261008193140_Abastecimiento`: agrega `Proveedor.PreciosFijos` (con un UPDATE que lo pone en 1 para SOALPRO y CARSA de la temporada abierta) y crea las 8 tablas, sin renombrar ni borrar nada.
> Migración `20261008235545_AbastecimientoAjuste1`: agrega `Proveedor.TrabajaConPedido` (1 para SOALPRO y CARSA de la temporada abierta), `PedidoDetalle.PrecioCompraUnidad`/`PrecioCatalogo` (backfill = precio actual del producto) y `Pedido.MontoTotalProveedor` (backfill = Σ cantidad × precio). Solo AddColumn + UPDATE.
> Migración `20261009004612_AbastecimientoAjuste2` (Up revisado a mano: crear → migrar datos → borrar): crea `Compra`, `CompraDetalle`, `CompraDistribucion`, `StockAlmacen` y el índice único `UX_Pedido_Proveedor_Codigo`; copia `LoteAlmacen` a `StockAlmacen` (suma por producto × almacén) y convierte las recepciones de proveedores sin `TrabajaConPedido` en compras a crédito (con sus movimientos); después borra `LoteAlmacen`, `MovimientoNav.LoteId`, `Proveedor.PreciosFijos` y `PedidoDetalle.PrecioCatalogo`. Al aplicarla las tablas del abastecimiento estaban vacías.
> Faltante (Σ pedido − Σ recibido, sin anuladas) y deuda se calculan, no tienen tabla. **Deuda (Ajuste 2)** por proveedor + código = Σ `MontoTotalProveedor` de pedidos activos + Σ compras no anuladas (van en la fila sin código) − Σ pagos activos. Las recepciones no generan deuda.
> Recepción: no puede superar lo pedido (proveedor + código + producto) y el precio sale de la línea del pedido.
> **Costo promedio ponderado** por producto y temporada = Σ (cantidad × precio) / Σ cantidad de las líneas de recepción y de compra no anuladas (`IStockNavService.ObtenerCostosPromedioAsync`). Sin lotes ni FIFO: las salidas usan `IStockNavService.RegistrarSalidaAsync`.

#### Esquema

```
Table Pedido {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProveedorId int [not null]
  CodigoClienteId int [null, note: 'Obligatorio si el proveedor UsaCodigosCliente; null si no']
  Fecha date [not null]
  Observacion nvarchar(500) [null]
  MontoTotalProveedor decimal(18,2) [not null, note: 'Monto que informa el proveedor; por defecto el calculado (Σ cantidad × PrecioCompraUnidad). Origen de la deuda si TrabajaConPedido']
}

Table PedidoDetalle {
  Id int [pk, increment]
  PedidoId int [not null, note: 'Cascade. Único (PedidoId, ProductoId)']
  ProductoId int [not null, note: 'Del proveedor del pedido']
  CantidadUnidades int [not null, note: '>= 0 (0 = el proveedor no lo tuvo). Con TrabajaConPedido, el total del código no baja de lo recibido']
  PrecioCompraUnidad decimal(18,2) [not null, note: 'Obligatorio, de la nota del código (> 0 si hay cantidad). Lo usan las recepciones. No actualiza el producto']
}

Table Recepcion {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProveedorId int [not null]
  CodigoClienteId int [null, note: 'Misma regla que en Pedido']
  NroFactura nvarchar(50) [null]
  Fecha date [not null]
  Observacion nvarchar(500) [null]
  Anulada bit [not null, note: 'Solo si el stock actual de cada almacén alcanza']
}

Table RecepcionDetalle {
  Id int [pk, increment]
  RecepcionId int [not null, note: 'Cascade']
  ProductoId int [not null]
  CantidadUnidades int [not null, note: 'Suma de su distribución']
  PrecioCompraUnidad decimal(18,2) [not null, note: 'Precio de la línea del pedido']
}

Table RecepcionDistribucion {
  Id int [pk, increment]
  RecepcionDetalleId int [not null, note: 'Cascade. Único (RecepcionDetalleId, AlmacenId)']
  AlmacenId int [not null, note: 'AuthDB (SistemaId = 2, activo), sin FK']
  CantidadUnidades int [not null, note: '> 0']
}

Table Compra {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProveedorId int [not null, note: 'Cualquier proveedor activo de la temporada']
  NroNota nvarchar(50) [null]
  Fecha date [not null]
  PagadaAlContado bit [not null]
  PagoProveedorId int [null, note: 'FK PagoProveedor (Restrict): pago automático por el total si PagadaAlContado']
  Observacion nvarchar(500) [null]
  Anulada bit [not null, note: 'Solo si el stock actual de cada almacén alcanza; anula también el pago automático']
}

Table CompraDetalle {
  Id int [pk, increment]
  CompraId int [not null, note: 'Cascade. Único (CompraId, ProductoId)']
  ProductoId int [not null, note: 'Cualquier producto activo de la temporada']
  CantidadUnidades int [not null, note: 'Suma de su distribución']
  PrecioCompraUnidad decimal(18,2) [not null, note: '> 0']
}

Table CompraDistribucion {
  Id int [pk, increment]
  CompraDetalleId int [not null, note: 'Cascade. Único (CompraDetalleId, AlmacenId)']
  AlmacenId int [not null, note: 'AuthDB (SistemaId = 2, activo), sin FK']
  CantidadUnidades int [not null, note: '> 0']
}

Table StockAlmacen {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProductoId int [not null, note: 'UX_StockAlmacen_Producto_Almacen (ProductoId, AlmacenId)']
  AlmacenId int [not null, note: 'AuthDB, sin FK']
  Cantidad int [not null, note: 'CK_StockAlmacen_CantidadNoNegativa: >= 0']
}

Table MovimientoNav {
  Id int [pk, increment]
  TemporadaId int [not null]
  AlmacenId int [not null, note: 'AuthDB, sin FK']
  ProductoId int [not null]
  Cantidad int [not null, note: '+ entrada / − salida']
  Tipo int [not null, note: 'TipoMovimientoNav: 1 Recepcion, 2 TrasladoSalida, 3 TrasladoEntrada, 4 Venta, 5 EntregaReserva, 6 Consumo, 7 SalidaRuta, 8 DevolucionRuta, 9 AjusteConteo, 10 AnulacionVenta, 11 AnulacionRecepcion, 12 Compra, 13 AnulacionCompra']
  ReferenciaTipo nvarchar(30) [not null, note: 'Recepcion, Compra, Venta, Traslado…']
  ReferenciaId int [not null]
  UsuarioId int [not null, note: 'AuthDB, sin FK']
  Fecha datetime2 [not null, note: 'UTC']
  Motivo nvarchar(500) [null]
}

Table PagoProveedor {
  Id int [pk, increment]
  TemporadaId int [not null]
  ProveedorId int [not null]
  CodigoClienteId int [null, note: 'Misma regla que en Pedido']
  Fecha date [not null]
  Monto decimal(18,2) [not null, note: '> 0']
  Medio int [not null, note: 'MedioPagoNav: 1 Transferencia, 2 Efectivo']
  Comprobante nvarchar(100) [null]
  Observacion nvarchar(500) [null]
}

Ref: Pedido.TemporadaId > Temporada.Id
Ref: Pedido.ProveedorId > Proveedor.Id
Ref: Pedido.CodigoClienteId > CodigoCliente.Id
Ref: PedidoDetalle.PedidoId > Pedido.Id
Ref: PedidoDetalle.ProductoId > Producto.Id
Ref: Recepcion.TemporadaId > Temporada.Id
Ref: Recepcion.ProveedorId > Proveedor.Id
Ref: Recepcion.CodigoClienteId > CodigoCliente.Id
Ref: RecepcionDetalle.RecepcionId > Recepcion.Id
Ref: RecepcionDetalle.ProductoId > Producto.Id
Ref: RecepcionDistribucion.RecepcionDetalleId > RecepcionDetalle.Id
Ref: Compra.TemporadaId > Temporada.Id
Ref: Compra.ProveedorId > Proveedor.Id
Ref: Compra.PagoProveedorId > PagoProveedor.Id
Ref: CompraDetalle.CompraId > Compra.Id
Ref: CompraDetalle.ProductoId > Producto.Id
Ref: CompraDistribucion.CompraDetalleId > CompraDetalle.Id
Ref: StockAlmacen.TemporadaId > Temporada.Id
Ref: StockAlmacen.ProductoId > Producto.Id
Ref: MovimientoNav.TemporadaId > Temporada.Id
Ref: MovimientoNav.ProductoId > Producto.Id
Ref: PagoProveedor.TemporadaId > Temporada.Id
Ref: PagoProveedor.ProveedorId > Proveedor.Id
Ref: PagoProveedor.CodigoClienteId > CodigoCliente.Id
```

---

## Estado de Implementación

| Módulo | Tabla | Estado Backend |
|--------|-------|----------------|
| **Catálogo** | | |
| | Categoria | ✅ |
| | Fabricante | ✅ |
| | DescripcionSabor | ✅ |
| | DescripcionTamanio | ✅ |
| | TipoPresentacion | ✅ |
| | Producto | ✅ |
| | ProductoVariante | ✅ |
| | ProductoPresentacion | ✅ |
| | HistorialPrecio | ⏳ |
| **Proveedores** | | |
| | Proveedor | ✅ |
| **Inventario** | | |
| | InventarioLote | ✅ |
| | InventarioLoteAlmacen | ✅ |
| | MovimientoInventario | ✅ |
| | Traslado (vía MovimientoInventario) | ✅ |
| **Compras** | | |
| | Compra | ✅ |
| | CompraDetalle | ✅ |
| | PagoCompra | ✅ |
| **Ventas y Caja** | | |
| | SesionCaja | ✅ |
| | Venta | 🟡 (endpoint de productos del POS pendiente) |
| | VentaDetalle | ✅ |
| | CategoriaGasto | ✅ |
| | GastoOperativo | ✅ |
| **Navidad — temporada-capital** (DB_NAVIDAD) | | |
| | Temporada | ✅ |
| | Inversor | ✅ |
| | AporteCapital | ✅ |
| | PagoInversor | ✅ |
| | CategoriaGastoNav | ✅ |
| | GastoNav | ✅ |
| | TemporadaAlmacenConteo | ✅ |
| **Navidad — catalogo** (DB_NAVIDAD) | | |
| | Proveedor | ✅ |
| | CodigoCliente | ✅ |
| | Producto (con alias y empaque) | ✅ |
| | ~~ProductoPresentacion~~ | eliminada (2026-10-08) |
| | Vendedor | ✅ |
| | ClienteNav | ✅ |
| | CategoriaProductoNav | ✅ |
| **Navidad — abastecimiento** (DB_NAVIDAD) | | |
| | Pedido / PedidoDetalle | ✅ |
| | Recepcion / RecepcionDetalle / RecepcionDistribucion | ✅ |
| | Compra / CompraDetalle / CompraDistribucion (Ajuste 2) | ✅ |
| | StockAlmacen (Ajuste 2, reemplaza LoteAlmacen) / MovimientoNav | ✅ |
| | PagoProveedor | ✅ |

---

## Notas Técnicas

### Auditoría de Fechas
Todos los campos `CreatedAt` usan `DateTime.UtcNow` en el backend. El frontend debe convertir a hora local (America/La_Paz para Bolivia).

### Campos sin FK
Los campos `UsuarioId`, `AlmacenId`, `CreatedByUsuarioId` son `int` simples que vienen del JWT token. NO tienen FK hacia DB_Auth.

### FIFO (First In, First Out)
El inventario usa método FIFO: los lotes más antiguos se venden primero. Cada `VentaDetalle` registra el `LoteId` específico del que salió la unidad.

### Conversión de presentaciones (unidad/paquete/caja)
El backend trabaja **siempre en unidades base**. La conversión de "cantidad en presentación" (ej. 2 cajas) a unidades base ocurre en el **frontend** (`compraHelpers.ts`, `useVentaHelpers.ts`, `inventarioHelpers.ts`) antes de llamar a la API — `CompraService`/`InventarioLoteService` reciben `CantidadUnidades` ya convertida y la persisten tal cual, sin revalidar contra `ProductoPresentacion.CantidadDePadre`. La única lógica de conversión en el backend (`ProductoPresentacionService.CalcularEquivalenciaRecursiva`) se usa solo para desglosar stock hacia la UI/reportes (`GET .../stock?stockEnUnidades=`), nunca para validar una compra o venta entrante. Documentado como deuda técnica en `MEMORIA.md` (raíz del proyecto).

### Enums del Sistema

| Enum | Valores | Uso |
|------|---------|-----|
| `EstadoPago` | 1:Pagado, 2:Credito, 3:ParcialmentePagado | Compra.EstadoPago |
| `TipoMovimiento` | 1:EntradaCompra, 2:SalidaVenta, 3:Traslado, 4:AjustePositivo, 5:AjusteNegativo | MovimientoInventario.TipoMovimiento |
| `MetodoPago` | 1:Efectivo, 2:TransferenciaQR, 3:Mixto | Venta.MetodoPago |
| `EstadoSesionCaja` | 1:Abierta, 2:Cerrada, 3:Ajustada | SesionCaja.Estado |
| `EstadoTemporada` (Navidad) | 1:Abierta, 2:Cerrada | Temporada.Estado |
| `TipoPagoInversor` (Navidad) | 1:Comision, 2:DevolucionCapital | PagoInversor.Tipo |
| `TipoVendedor` (Navidad) | 1:Tienda, 2:Ruta | Vendedor.Tipo |