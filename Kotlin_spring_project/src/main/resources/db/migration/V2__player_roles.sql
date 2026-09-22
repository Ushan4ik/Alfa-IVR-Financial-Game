ALTER TABLE players
    ADD COLUMN IF NOT EXISTS role varchar(32) NOT NULL DEFAULT 'PLAYER';

ALTER TABLE players
    ADD CONSTRAINT chk_players_role CHECK (role IN ('PLAYER', 'ADMIN'));
