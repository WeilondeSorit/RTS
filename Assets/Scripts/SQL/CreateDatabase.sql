-- Удаляем все таблицы в правильном порядке (с учетом зависимостей)
DROP TABLE IF EXISTS Healing CASCADE;
DROP TABLE IF EXISTS Attack CASCADE;
DROP TABLE IF EXISTS Construction CASCADE;
DROP TABLE IF EXISTS UnitAction CASCADE;
DROP TABLE IF EXISTS MapCell CASCADE;
DROP TABLE IF EXISTS GameMap CASCADE;
DROP TABLE IF EXISTS DefenseTower CASCADE;
DROP TABLE IF EXISTS Farm CASCADE;
DROP TABLE IF EXISTS MainBuilding CASCADE;
DROP TABLE IF EXISTS Building CASCADE;
DROP TABLE IF EXISTS Healer CASCADE;
DROP TABLE IF EXISTS Villager CASCADE;
DROP TABLE IF EXISTS Archer CASCADE;
DROP TABLE IF EXISTS Unit CASCADE;
DROP TABLE IF EXISTS Achievement CASCADE;
DROP TABLE IF EXISTS Settings CASCADE;
DROP TABLE IF EXISTS player_data CASCADE;
DROP TABLE IF EXISTS Player CASCADE;
-- я уже ненавижу этот файл. по нему была вся курсовая. даже не знаю, допишу ли ее
-- Таблица Игрок (основная сущность)
CREATE TABLE Player (
    id SERIAL PRIMARY KEY,
    login VARCHAR(50) NOT NULL,
    password VARCHAR(50) NOT NULL,
    wins INTEGER DEFAULT 0 CHECK (wins >= 0),
    losses INTEGER DEFAULT 0 CHECK (losses >= 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица Настройки (зависит от Игрока)
CREATE TABLE Settings (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    sound_on BOOLEAN DEFAULT TRUE,
    volume DECIMAL(3,1) DEFAULT 100.0 CHECK (volume >= 0 AND volume <= 100),
    FOREIGN KEY (player_id) REFERENCES Player(id) ON DELETE CASCADE
);

-- Таблица Достижения (игрок получает достижения)
CREATE TABLE Achievement (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    achievement_name VARCHAR(100) NOT NULL,
    is_achieved BOOLEAN DEFAULT FALSE,
    achieved_at TIMESTAMP,
    FOREIGN KEY (player_id) REFERENCES Player(id) ON DELETE CASCADE
);

-- Таблица игровых данных (ресурсы игрока)
CREATE TABLE player_data (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    units INTEGER DEFAULT 0 CHECK (units >= 0),
    food INTEGER DEFAULT 0 CHECK (food >= 0),
    wood INTEGER DEFAULT 0 CHECK (wood >= 0),
    rock INTEGER DEFAULT 0 CHECK (rock >= 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (player_id) REFERENCES Player(id) ON DELETE CASCADE
);

-- Основная таблица Юнит (родительская для всех юнитов)
CREATE TABLE Unit (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    unit_type VARCHAR(50) NOT NULL,
    coord_x INTEGER NOT NULL CHECK (coord_x >= 0),
    coord_y INTEGER NOT NULL CHECK (coord_y >= 0),
    current_health INTEGER NOT NULL CHECK (current_health >= 0),
    max_health INTEGER NOT NULL CHECK (max_health > 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (player_id) REFERENCES Player(id) ON DELETE CASCADE,
    CHECK (current_health <= max_health)
);

-- Таблица Лучник (специализация Юнита)
CREATE TABLE Archer (
    id INTEGER PRIMARY KEY,
    attack_power INTEGER NOT NULL CHECK (attack_power > 0),
    attack_speed DECIMAL(5,2) NOT NULL CHECK (attack_speed > 0),
    attack_range INTEGER NOT NULL CHECK (attack_range > 0),
    is_friendly BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id) REFERENCES Unit(id) ON DELETE CASCADE
);

-- Таблица Деревенский (специализация Юнита)
CREATE TABLE Villager (
    id INTEGER PRIMARY KEY,
    is_busy BOOLEAN DEFAULT FALSE,
    work_type VARCHAR(50),
    work_target_id INTEGER,
    efficiency DECIMAL(4,2) DEFAULT 1.0 CHECK (efficiency >= 0 AND efficiency <= 2.0),
    FOREIGN KEY (id) REFERENCES Unit(id) ON DELETE CASCADE
);

-- Таблица Лекарь (специализация Юнита)
CREATE TABLE Healer (
    id INTEGER PRIMARY KEY,
    heal_amount INTEGER NOT NULL CHECK (heal_amount > 0),
    heal_speed DECIMAL(5,2) NOT NULL CHECK (heal_speed > 0),
    heal_range INTEGER NOT NULL CHECK (heal_range > 0),
    is_friendly BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id) REFERENCES Unit(id) ON DELETE CASCADE
);

-- Основная таблица Здание (родительская для всех зданий)
CREATE TABLE Building (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL,
    building_type VARCHAR(50) NOT NULL,
    coord_x INTEGER NOT NULL CHECK (coord_x >= 0),
    coord_y INTEGER NOT NULL CHECK (coord_y >= 0),
    current_health INTEGER NOT NULL CHECK (current_health >= 0),
    max_health INTEGER NOT NULL CHECK (max_health > 0),
    level INTEGER DEFAULT 1 CHECK (level >= 1),
    built_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (player_id) REFERENCES Player(id) ON DELETE CASCADE,
    CHECK (current_health <= max_health)
);

-- Таблица Главное здание (специализация Здания)
CREATE TABLE MainBuilding (
    id INTEGER PRIMARY KEY,
    population_capacity INTEGER NOT NULL CHECK (population_capacity > 0),
    can_produce_units BOOLEAN DEFAULT TRUE,
    is_friendly BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id) REFERENCES Building(id) ON DELETE CASCADE
);

-- Таблица Ферма (специализация Здания)
CREATE TABLE Farm (
    id INTEGER PRIMARY KEY,
    production_rate INTEGER NOT NULL CHECK (production_rate >= 0),
    resource_type VARCHAR(50) NOT NULL,
    storage_capacity INTEGER NOT NULL CHECK (storage_capacity > 0),
    current_storage INTEGER DEFAULT 0 CHECK (current_storage >= 0),
    FOREIGN KEY (id) REFERENCES Building(id) ON DELETE CASCADE,
    CHECK (current_storage <= storage_capacity)
);

-- Таблица Защитная башня (специализация Здания)
CREATE TABLE DefenseTower (
    id INTEGER PRIMARY KEY,
    damage INTEGER NOT NULL CHECK (damage > 0),
    attack_speed DECIMAL(5,2) NOT NULL CHECK (attack_speed > 0),
    attack_range INTEGER NOT NULL CHECK (attack_range > 0),
    can_attack_air BOOLEAN DEFAULT FALSE,
    is_friendly BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id) REFERENCES Building(id) ON DELETE CASCADE
);

-- Таблица Карта (содержит все объекты)
CREATE TABLE GameMap (
    id SERIAL PRIMARY KEY,
    map_name VARCHAR(100) NOT NULL,
    width INTEGER NOT NULL CHECK (width > 0),
    height INTEGER NOT NULL CHECK (height > 0),
    max_players INTEGER DEFAULT 4 CHECK (max_players >= 2 AND max_players <= 8),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица клеток карты (позиции объектов)
CREATE TABLE MapCell (
    id SERIAL PRIMARY KEY,
    map_id INTEGER NOT NULL,
    coord_x INTEGER NOT NULL CHECK (coord_x >= 0),
    coord_y INTEGER NOT NULL CHECK (coord_y >= 0),
    terrain_type VARCHAR(50) NOT NULL,
    unit_id INTEGER,
    building_id INTEGER,
    resource_type VARCHAR(50),
    resource_amount INTEGER DEFAULT 0 CHECK (resource_amount >= 0),
    is_passable BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (map_id) REFERENCES GameMap(id) ON DELETE CASCADE,
    FOREIGN KEY (unit_id) REFERENCES Unit(id) ON DELETE SET NULL,
    FOREIGN KEY (building_id) REFERENCES Building(id) ON DELETE SET NULL
);

-- Таблица действий (юнит выполняет действие)
CREATE TABLE UnitAction (
    id SERIAL PRIMARY KEY,
    unit_id INTEGER NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_unit_id INTEGER,
    target_building_id INTEGER,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP,
    is_completed BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (unit_id) REFERENCES Unit(id) ON DELETE CASCADE,
    FOREIGN KEY (target_unit_id) REFERENCES Unit(id) ON DELETE SET NULL,
    FOREIGN KEY (target_building_id) REFERENCES Building(id) ON DELETE SET NULL
);

-- Таблица строительства (деревенский строит здание)
CREATE TABLE Construction (
    id SERIAL PRIMARY KEY,
    villager_id INTEGER NOT NULL,
    building_id INTEGER NOT NULL,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completion_time TIMESTAMP,
    progress_percentage DECIMAL(5,2) DEFAULT 0.0 CHECK (progress_percentage >= 0 AND progress_percentage <= 100),
    required_food INTEGER DEFAULT 0,
    required_wood INTEGER DEFAULT 0,
    required_rock INTEGER DEFAULT 0,
    FOREIGN KEY (villager_id) REFERENCES Villager(id) ON DELETE CASCADE,
    FOREIGN KEY (building_id) REFERENCES Building(id) ON DELETE CASCADE
);

-- Таблица атак (лучник/башня атакуют цель)
CREATE TABLE Attack (
    id SERIAL PRIMARY KEY,
    attacker_unit_id INTEGER,
    attacker_building_id INTEGER,
    target_unit_id INTEGER,
    target_building_id INTEGER,
    damage_dealt INTEGER NOT NULL CHECK (damage_dealt >= 0),
    attack_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_critical BOOLEAN DEFAULT FALSE,
    attack_type VARCHAR(50),
    FOREIGN KEY (attacker_unit_id) REFERENCES Unit(id) ON DELETE SET NULL,
    FOREIGN KEY (attacker_building_id) REFERENCES Building(id) ON DELETE SET NULL,
    FOREIGN KEY (target_unit_id) REFERENCES Unit(id) ON DELETE SET NULL,
    FOREIGN KEY (target_building_id) REFERENCES Building(id) ON DELETE SET NULL,
    CHECK (
        (attacker_unit_id IS NOT NULL OR attacker_building_id IS NOT NULL) AND
        (target_unit_id IS NOT NULL OR target_building_id IS NOT NULL)
    )
);

-- Таблица лечения (лекарь лечит юнита)
CREATE TABLE Healing (
    id SERIAL PRIMARY KEY,
    healer_id INTEGER NOT NULL,
    target_unit_id INTEGER NOT NULL,
    heal_amount INTEGER NOT NULL CHECK (heal_amount > 0),
    heal_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    heal_type VARCHAR(50),
    FOREIGN KEY (healer_id) REFERENCES Healer(id) ON DELETE CASCADE,
    FOREIGN KEY (target_unit_id) REFERENCES Unit(id) ON DELETE CASCADE
);