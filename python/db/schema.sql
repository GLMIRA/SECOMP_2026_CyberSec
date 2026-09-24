PRAGMA foreign_keys = ON;

CREATE TABLE usuarios (
    id      INTEGER PRIMARY KEY,
    usuario TEXT NOT NULL UNIQUE,
    senha   TEXT NOT NULL,
    admin   INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE perfis (
    usuario_id INTEGER PRIMARY KEY,
    nome       TEXT,
    cpf        TEXT UNIQUE,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE contas (
    id         INTEGER PRIMARY KEY,
    usuario_id INTEGER NOT NULL,
    saldo      REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE transferencias (
    id            INTEGER PRIMARY KEY,
    conta_origem  INTEGER NOT NULL,
    conta_destino INTEGER NOT NULL,
    valor         REAL NOT NULL,
    data          TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (conta_origem)  REFERENCES contas(id),
    FOREIGN KEY (conta_destino) REFERENCES contas(id)
);

CREATE TABLE movimentacoes (
    id       INTEGER PRIMARY KEY,
    conta_id INTEGER NOT NULL,
    tipo     TEXT NOT NULL,
    valor    REAL NOT NULL,
    data     TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (conta_id) REFERENCES contas(id)
);

INSERT INTO usuarios (id, usuario, senha, admin) VALUES
    (1, 'alice', 'alice123', 0),
    (2, 'bob',   'bob123',   0);

INSERT INTO perfis (usuario_id, nome, cpf) VALUES
    (1, 'Alice Silva', '111.111.111-11'),
    (2, 'Bob Souza',   '222.222.222-22');

INSERT INTO contas (id, usuario_id, saldo) VALUES
    (1, 1, 1000),
    (2, 2, 1000);
