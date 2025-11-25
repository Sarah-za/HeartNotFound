-- Tabelle für Patienten anlegen
CREATE TABLE patients (
	pid 	int PRIMARY KEY,
	name 	VARCHAR(80),
	vorname VARCHAR(80));
	
-- Patientendaten anlegen
INSERT INTO patients VALUES 
	(1,'Maier','Helmut'),
	(2,'Mueller','Hanne'),
	(3,'Becker','Barbara'),
	(4,'Kleber','Helmut');
	
-- Alle aktuellen Patienten ausgeben
SELECT * FROM Patients;

-- einen neuen Patienten einfügen
INSERT INTO patients VALUES 
	(5,'Gause','Giesela');
	
	
-- Tabelle für Patientenmonitore anlegen
CREATE TABLE monitors (
	moid 	int PRIMARY KEY,
	model 	VARCHAR(80)

);
	
-- einige Monitormodelle einfügen
INSERT INTO monitors VALUES 
	(1,'Philips IntelliView'),
	(2,'Philips IntelliView'),
	(3,'Mindray BeneVision'),
	(4,'Mindray BeneVision');
	
-- alle Monitor ausgeben
SELECT * FROM monitors;

-- Tabelle mit der aktuellen Belegung von Monitoren mit Patienten
CREATE TABLE belegung (
	moid int REFERENCES monitors(moid),
	pid int REFERENCES patients(pid));
	
-- Daten mit aktueller Belegung der Monitor
INSERT INTO belegung VALUES
	(1,3),
	(2,1);
	
SELECT * FROM belegung;


-- Jetzt alle aktiven Monitore und Patientennamen ausgeben
select monitors.moid, patients.name, patients.vorname FROM patients NATURAL JOIN belegung NATURAL JOIN monitors;

-- Alternative Schreibweise
SELECT monitors.moid, patients.name, patients.vorname FROM patients,belegung,monitors WHERE patients.pid=belegung.pid AND monitors.moid=belegung.moid;
	

-- Neuer Patient kommt und wird an Montitor 1 angeschlossen
INSERT INTO patients VALUES (6,'Breitner','Paul');
UPDATE belegung SET pid=6 WHERE moid=1;

-- Patient geht und Monitor ist gerade nicht in Betrieb
DELETE FROM belegung where moid=1;



	
	