ALTER TABLE players
    ADD COLUMN coins integer NOT NULL DEFAULT 0;

ALTER TABLE players
    ADD CONSTRAINT chk_players_coins_non_negative CHECK (coins >= 0);

CREATE TABLE streak_reward_claims (
    id varchar(255) NOT NULL PRIMARY KEY,
    player_id varchar(255) NOT NULL,
    milestone_days integer NOT NULL,
    claimed_at timestamp with time zone NOT NULL,
    CONSTRAINT uq_streak_reward_player_milestone UNIQUE (player_id, milestone_days),
    CONSTRAINT fk_streak_reward_player
        FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
);

CREATE INDEX idx_streak_reward_claim_player ON streak_reward_claims (player_id);
