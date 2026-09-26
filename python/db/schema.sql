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

-- Clientes extras (demo de enumeracao de contas / IDOR). Contas com ids "espalhados",
-- incluindo o id 12 (conta com saldo alto = "premio" pra achar no Intruder).
INSERT INTO usuarios (id, usuario, senha, admin) VALUES
    (3,  'joao',   'senha03', 0),
    (4,  'maria',  'senha04', 0),
    (5,  'pedro',  'senha05', 0),
    (6,  'ana',    'senha06', 0),
    (7,  'carlos', 'senha07', 0),
    (8,  'luiz',   'senha08', 0),
    (9,  'julia',  'senha09', 0),
    (10, 'rafael', 'senha10', 0),
    (11, 'bruna',  'senha11', 0),
    (12, 'felipe', 'senha12', 0);

INSERT INTO perfis (usuario_id, nome, cpf) VALUES
    (3,  'Joao Souza',     '300.000.000-03'),
    (4,  'Maria Oliveira', '400.000.000-04'),
    (5,  'Pedro Santos',   '500.000.000-05'),
    (6,  'Ana Costa',      '600.000.000-06'),
    (7,  'Carlos Lima',    '700.000.000-07'),
    (8,  'Luiz Fernandes', '800.000.000-08'),
    (9,  'Julia Almeida',  '900.000.000-09'),
    (10, 'Rafael Rocha',   '100.000.000-10'),
    (11, 'Bruna Martins',  '110.000.000-11'),
    (12, 'Felipe Ramos',   '120.000.000-12');

INSERT INTO contas (id, usuario_id, saldo) VALUES
    (3,  3,  1500),
    (4,  4,  320),
    (6,  5,  8000),
    (7,  6,  50),
    (9,  7,  12000),
    (12, 8,  5000000),
    (14, 9,  700),
    (17, 10, 240),
    (19, 11, 5000),
    (22, 12, 99);
