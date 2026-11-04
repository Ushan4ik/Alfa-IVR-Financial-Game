CREATE TABLE daily_progress (
    player_id varchar(255) NOT NULL PRIMARY KEY,
    activity_date date NOT NULL,
    current_streak integer NOT NULL DEFAULT 0,
    best_streak integer NOT NULL DEFAULT 0,
    tests_completed integer NOT NULL DEFAULT 0,
    mini_games_played integer NOT NULL DEFAULT 0,
    CONSTRAINT fk_daily_progress_player
        FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
);
