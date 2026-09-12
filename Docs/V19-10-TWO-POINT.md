# V19.10 – Physische Zwei-Punkt-Ausrichtung

## Ziel und Grenzen

Raumgeometrie (Live-TSDF) und Wiederherstellen ihrer physischen Lage sind getrennt.
Neue Profile benötigen keinen lokalisierten Meta-Spatial-Anchor. Die Spielerpose
ist keine Eingabe der Ausrichtungsrechnung. Zwei geordnete reale Punkte und die
Schwerkraftrichtung liefern Translation und Yaw; keine Skalierung und kein Kippen.
Eine falsche Zuordnung gleich weit entfernter Punkte ist mathematisch nicht allein
an zwei Messungen erkennbar. Deshalb bleibt die sichtbare Netzprüfung obligatorisch.
Automatische Ankerlokalisierung/Markererkennung ist in dieser Version kein Ladepfad.

## Bedienung

1. Einmal neu scannen, Scan wie bisher bestätigen, am Schrein „Raum speichern“ wählen.
2. Zwei unveränderliche, bequem erreichbare Merkmale an derselben Wand wählen:
   A links, B rechts, mindestens 1,2 m horizontal auseinander, möglichst weiter.
   Keine beweglichen Möbel, kein Spielzeug. Exakt dieselben Stellen/Höhen merken.
3. Die kleine sichtbare Controller-Spitze jeweils dicht an den realen Punkt führen,
   kurz ruhig halten (grün), A drücken; bei linker Waffenhand X. Keine Wandberührung
   erzwingen. Der Abzug ist während dieser Phase kein Aufnahme-/Schussauslöser.
4. B bzw. Y setzt bei Schritt B den Punkt A zurück; bei Schritt A bricht es ab.
5. Nach beiden Punkten wird die Karte gespeichert. Wiederholtes Speichern desselben
   korrekt ausgerichteten Raums behält seine Bezugspunkte.
6. Beim Start „Raum … laden“, dieselben A/B-Punkte markieren, goldenes Netz an
   mehreren Wänden und am Boden prüfen, „Raum verwenden“. Keine parallele blaue
   Rekonstruktion in dieser Phase. Zum Start in der gespeicherten freien Fläche stehen.
7. Bei falscher Vorschau „Punkte erneut setzen“. Im pausierten Spiel: Schrein →
   Raummenü → Raumoptionen → Raum neu ausrichten. Das lädt den gespeicherten Stand
   neu und setzt die Runde/ungespeicherte Raumänderungen nach Bestätigung zurück.

Alte V1-Profile werden weiterhin gelesen, aber ohne A/B nicht geraten ausgerichtet.
Sie bleiben auf dem Gerät unverändert. Einmal neu scannen und in einem freien Slot
speichern oder ausdrücklich ersetzen; beim Ersetzen bleibt die Vorgängerversion
als `.previous` erhalten. Zwei Punkte ersetzen nicht die Möbel-/Hinderniserfassung.

## Technische Prüfpunkte

- Metadaten V2 mit calibrationVersion, kanonischen Punkten A/B; V1 lesbar.
- Endpunktmessung direkt am getrackten Controller, nicht auf gespeichertem Mesh.
- 400 ms Punktstabilität, 18 mm Bewegungsfenster, Neutral-/Flankengate.
- Mindestabstand 1,2 m; Span-Toleranz 2 % (6–12 cm), relative Höhe max. 7 cm.
- Während Aufnahme und Datei-I/O ruhen Discovery, Fusion und Scanner-UI.
- Import in einem gemeinsamen starren Frame für Rendergeometrie, Collider, Boden
  und Schrein. Goldvorschau ist unbestätigt, bis Nutzer zustimmt und Mindestfläche passt.
- Tracking-/Fokuswechsel verwirft laufende Ausrichtung. Kein Nachführen am Kopf.
- Aufnahmeabbruch beim Speichern erhält den Scan; Ladeabbruch entfernt Teilimporte.
- Nach Annahme sind normale begrenzte Live-Updates für aktuelle Hindernisse wieder aktiv.

## Abnahme

Automatisiert: Transformationen über mehrere Ursprünge/Yaws; unbekannte freie
Flächen; reale Save/Load-Coroutinen; Profilintegrität; Abbruch/Trackingverlust;
Links-/Rechtshändertexte; Regressionen für Raum, Gameplay und Eingaben.

Auf Quest noch erforderlich: einmal speichern, dann mindestens drei vollständige
App-Neustarts von verschiedenen Standpositionen/Blickrichtungen, dieselben A/B
setzen und je eine dritte unabhängige Wandecke samt Boden kontrollieren. Auch
vertauschte Punkte, zu kleiner Abstand, Handtracking-Verlust, Fokuswechsel,
Neuausrichten und veränderte Möbel prüfen. Automatische Tests ersetzen diese
physische Abnahme nicht.

## Build

`bash Tools/build-v19.10.sh`; Ziel `Builds/QuestDemonMR-v19.10-two-point.apk`,
Version 0.19.10 / Android-Code 49. Prüfergebnisse in `Verification/TwoPoint`.

## Ergebnis vom 07.09.2026

- 2.617 automatische Prüfungen bestanden, davon 134 neue Kalibrierungsprüfungen.
  Der Save/Load-Test verwirft zwischen Ladevorgängen auch den RoomProfiles-Manager
  und prüft frische Instanzen mit unterschiedlichen Tracking-Ursprüngen.
- Links-/Rechtshänder-Ansicht gerendert; linke Ansicht visuell geprüft:
  `Verification/TwoPoint/calibration-left.png`. Kein überlappender Anleitungstext.
- Unity-Export ohne C#-/Shaderfehler oder Exceptions; Android/IL2CPP-Build erfolgreich.
  Finaler Export: `/private/tmp/qdmr-v1910-export.Q8NFrP`.
- APK: 106.629.010 Bytes; ZIP-Integrität und APK-v2-Signatur geprüft.
- APK-SHA256: `4a95e38376e9b3b787f6da42d372d3e62a807859732431feba0cf6c3016894c2`.
- Enthaltene `libil2cpp.so` stimmt mit dem frisch gebauten Native-Artefakt überein:
  `8aa5cfb61842a02beeef1b8fda07f36491b4fddfd1637e07998cc14915fdf101`.
- Signatur entspricht V19.9, Updateinstallation ohne Deinstallation möglich.
- In diesem Arbeitsschritt nicht auf der Quest installiert. Physische A/B-Präzision,
  Controller-Ergonomie, Tracking-Drift und Kaltstarts auf dem getragenen Headset
  sind ausdrücklich noch nicht abgenommen.
