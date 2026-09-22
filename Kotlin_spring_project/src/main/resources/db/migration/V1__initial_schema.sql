CREATE TABLE holdings (
    id varchar(255) NOT NULL PRIMARY KEY,
    avg_price double precision NOT NULL,
    player_id varchar(255),
    quantity double precision NOT NULL,
    stock_id varchar(255),
    CONSTRAINT uq_holdings_player_stock UNIQUE (player_id, stock_id)
);

CREATE TABLE learning_attempts (
    id varchar(255) NOT NULL PRIMARY KEY,
    awarded_score integer NOT NULL,
    created_at timestamp with time zone,
    diagnostic boolean NOT NULL,
    finished boolean NOT NULL,
    lesson_id varchar(255),
    parent_lesson_id varchar(255),
    player_id varchar(255),
    questions_json text
);
CREATE INDEX idx_learning_attempts_player_id ON learning_attempts (player_id);

CREATE TABLE market_orders (
    id varchar(255) NOT NULL PRIMARY KEY,
    created_at timestamp with time zone,
    day integer NOT NULL,
    player_id varchar(255),
    price double precision NOT NULL,
    quantity double precision NOT NULL,
    stock_id varchar(255),
    type varchar(255),
    CONSTRAINT chk_market_order_type CHECK (type IN ('BUY', 'SELL', 'SHORT', 'COVER'))
);

CREATE TABLE market_state (
    id varchar(255) NOT NULL PRIMARY KEY,
    current_day integer NOT NULL,
    last_tick_at timestamp with time zone,
    tick_interval_minutes integer NOT NULL
);

CREATE TABLE news_events (
    id varchar(255) NOT NULL PRIMARY KEY,
    created_at timestamp with time zone,
    day integer NOT NULL,
    headline varchar(255),
    sector varchar(255),
    sentiment integer NOT NULL,
    CONSTRAINT chk_news_sector CHECK (sector IN ('MINING', 'TECH', 'ENERGY', 'FINANCE', 'RETAIL', 'AGRICULTURE'))
);

CREATE TABLE personal_lessons (
    id varchar(255) NOT NULL PRIMARY KEY,
    attempt_id varchar(255),
    completed boolean NOT NULL,
    content_json text,
    created_at timestamp with time zone,
    player_id varchar(255),
    status varchar(255),
    topic_id varchar(255),
    CONSTRAINT uq_personal_lessons_attempt UNIQUE (attempt_id)
);
CREATE INDEX idx_personal_lessons_player_id ON personal_lessons (player_id);

CREATE TABLE player_progress (
    player_id varchar(255) NOT NULL PRIMARY KEY,
    fin_literacy_score integer NOT NULL,
    last_updated timestamp with time zone,
    level integer NOT NULL
);

CREATE TABLE players (
    id varchar(255) NOT NULL PRIMARY KEY,
    cash double precision NOT NULL,
    created_at timestamp with time zone,
    email varchar(255) NOT NULL,
    interests varchar(255),
    nickname varchar(255),
    password_hash varchar(255) NOT NULL,
    CONSTRAINT uq_players_email UNIQUE (email)
);

CREATE TABLE price_points (
    id varchar(255) NOT NULL PRIMARY KEY,
    day integer NOT NULL,
    price double precision NOT NULL,
    stock_id varchar(255)
);

CREATE TABLE questions (
    id varchar(255) NOT NULL PRIMARY KEY,
    answers varchar(255),
    category varchar(255),
    correct_index integer NOT NULL,
    lesson_id varchar(255),
    level integer NOT NULL,
    text varchar(255)
);

CREATE TABLE stocks (
    id varchar(255) NOT NULL PRIMARY KEY,
    base_price double precision NOT NULL,
    current_price double precision NOT NULL,
    name varchar(255),
    pending_pressure double precision NOT NULL,
    previous_price double precision NOT NULL,
    sector varchar(255),
    ticker varchar(255),
    volatility double precision NOT NULL,
    CONSTRAINT chk_stock_sector CHECK (sector IN ('MINING', 'TECH', 'ENERGY', 'FINANCE', 'RETAIL', 'AGRICULTURE'))
);
