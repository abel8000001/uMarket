-- CREACIÓN DE TABLAS

CREATE TABLE Tipo_Usuario (
  id_tipo_usuario INTEGER PRIMARY KEY,
  nombre_tipo_usuario TEXT NOT NULL
);

CREATE TABLE Usuarios (
  id_institucional INTEGER PRIMARY KEY,
  documento TEXT NOT NULL,
  nombre TEXT NOT NULL,
  apellido TEXT NOT NULL,
  edad INTEGER,
  correo_institucional TEXT NOT NULL,
  id_tipo_usuario INTEGER NOT NULL,
  FOREIGN KEY (id_tipo_usuario) REFERENCES Tipo_Usuario(id_tipo_usuario)
);

CREATE TABLE CredencialesUsuario (
  id_institucional INTEGER PRIMARY KEY,
  password_hash TEXT NOT NULL,
  created_at DATETIME DEFAULT (datetime('now')),
  FOREIGN KEY (id_institucional) REFERENCES Usuarios(id_institucional)
);

CREATE TABLE Categoria_Tienda (
  id_categoria_tienda INTEGER PRIMARY KEY,
  nombre_categoria TEXT NOT NULL
);

CREATE TABLE Tiendas (
  id_tienda INTEGER PRIMARY KEY,
  nombre_tienda TEXT NOT NULL,
  id_institucional_propietario INTEGER NOT NULL,
  descripcion TEXT,
  imagen TEXT,
  id_categoria_tienda INTEGER NOT NULL,
  FOREIGN KEY (id_institucional_propietario) REFERENCES Usuarios(id_institucional),
  FOREIGN KEY (id_categoria_tienda) REFERENCES Categoria_Tienda(id_categoria_tienda)
);

CREATE TABLE Productos (
  id_producto INTEGER PRIMARY KEY,
  nombre TEXT NOT NULL,
  precio INTEGER NOT NULL,
  descripcion TEXT,
  imagen TEXT
);

CREATE TABLE Tienda_Productos (
  id_tienda INTEGER NOT NULL,
  id_producto INTEGER NOT NULL,
  PRIMARY KEY (id_tienda, id_producto),
  FOREIGN KEY (id_tienda) REFERENCES Tiendas(id_tienda),
  FOREIGN KEY (id_producto) REFERENCES Productos(id_producto)
);

CREATE TABLE Pedidos (
  id_pedido INTEGER PRIMARY KEY,
  id_tienda INTEGER NOT NULL,
  id_producto INTEGER NOT NULL,
  estado_pedido TEXT NOT NULL,
  descripcion TEXT,
  FOREIGN KEY (id_tienda) REFERENCES Tiendas(id_tienda),
  FOREIGN KEY (id_producto) REFERENCES Productos(id_producto)
);
