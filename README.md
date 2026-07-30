# HeartNotFound

Remote-Monitoring-System zur Überwachung von Vitalparametern (Herzfrequenz, Atemfrequenz, Temperatur, Blutdruck, SpO2) von Patienten. Entwickelt als studentisches Projekt im Rahmen der Vorlesung PMS (Technische Hochschule Mannheim, WS 2025).

## Über das Projekt

HeartNotFound ermöglicht medizinischem Fachpersonal die gleichzeitige Fernüberwachung von bis zu 8 Patienten. Vitaldaten (Herzfrequenz, Atemfrequenz, Temperatur, Blutdruck, SpO2) werden erfasst, visualisiert und bei kritischen Grenzwertüberschreitungen wird automatisch alarmiert (2-stufige Alarmierung inkl. Early-Warning-Score).

**Hinweis:** Klassifiziert als Medizinprodukt der Klasse IIa (MDR). Ersetzt keine ärztliche Bewertung und ist nicht für Patienten geeignet, die eine zwingende Vor-Ort-Überwachung benötigen.

## Komponenten

Das System besteht aus drei eigenständigen Anwendungen:

- **Administration** – Verwaltung von Patienten, Monitoren und deren Zuordnung
- **Vitaldatensimulator** – Erzeugt simulierte Vitaldaten für Test- und Demozwecke (kein Bestandteil des eigentlichen Medizinprodukts)
- **Remote-Monitor** – Zentrale Überwachungsansicht mit Alarmierung und Verlaufsdarstellung

Die Kommunikation zwischen den Komponenten erfolgt über **MQTT**, die Persistenz über eine **PostgreSQL**-Datenbank.

## Tech-Stack

- .NET 8.0
- MQTT (MQTTnet)
- PostgreSQL (Npgsql)
- MSTest

## Voraussetzungen

- MQTT-Broker und PostgreSQL-Datenbank erreichbar
- .NET 8.0 installiert
- Konfigurationsdateien für Datenbank- und MQTT-Verbindung (`dbconfig.txt`, `mqttconfig.txt`)

## Installation & Start

1. Repository klonen bzw. Release-Pakete herunterladen
2. Für jede Komponente (`AdminDemo`, `SimInstallDemo`, `RemoteMonitorDemo`) die enthaltene `.exe` starten
3. Beim Start jeweils die Konfigurationsdatei laden (Button „Load config“) oder Zugangsdaten manuell eintragen

**Empfohlener Ablauf:**

1. Administration starten → Patient anlegen → Monitor zuweisen
2. Vitaldatensimulator starten → zugewiesene Monitor-ID eintragen → MQTT-Konfiguration laden
3. Remote-Monitor starten → Konfigurationen laden → Patient erscheint automatisch in der Liste

## Team

Entwickelt von einem 5-köpfigen Studierendenteam im Rahmen des Projekts Medizinische Software-Entwicklung.
