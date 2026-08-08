CONNECT parking_user/ParkingPassword123!@localhost:1521/FREEPDB1;

CREATE TABLE ParkingSpots (
    Id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY ,
    Code VARCHAR2(10) NOT NULL UNIQUE,
    SpotType VARCHAR2(20) DEFAULT 'STANDARD' NOT NULL,
    CONSTRAINT chk_spot_type CHECK (SpotType IN ('STANDARD', 'DISABLED', 'EV_CHARGING', 'VIP'))
);

CREATE TABLE Reservations(
    Id VARCHAR2(36) PRIMARY KEY,
    ParkingSpotId  NUMBER NOT NULL,
    RequesterEmail VARCHAR2(100) NOT NULL,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE NOT NULL,
    CONSTRAINT fk_parkingspot FOREIGN KEY (ParkingSpotId) REFERENCES ParkingSpots(Id),
    CONSTRAINT chk_time_range CHECK (EndTime > StartTime)
);

CREATE INDEX idx_res_spot_time ON Reservations(ParkingSpotId,StartTime,EndTime);

INSERT INTO ParkingSpots (Code) VALUES ('A-01');
INSERT INTO ParkingSpots (Code,SpotType) VALUES ('A-02','DISABLED');
INSERT INTO ParkingSpots (Code,SpotType) VALUES ('B-01','EV_CHARGING');
INSERT INTO ParkingSpots (Code,SpotType) VALUES ('C-01','VIP');

COMMIT;


