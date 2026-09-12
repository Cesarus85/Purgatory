# V19.8 – belastbarer Raum-Lebenszyklus

Gemeldetes Fehlerbild: gespeicherte Karte nach Neustart versetzt, gleichzeitiger neuer Scan, keine verständliche Freigabe oder Abbruchmöglichkeit.

## Befund und Änderungen

- Profildatei auf angeschlossener Quest vorhanden; vor Änderungen lokal gesichert. Keine Löschung oder Änderung des Geräteprofils.
- Bisher: erste lokalisierbare Ankerpose bewegt Kamera-Rig in alte Weltkoordinaten; keine stabile Pose abgewartet. Während danach frisch rekonstruiert wird, bleibt alte goldene Geometrie daneben sichtbar. Messfehler werden einmalig gesammelt und können das begrenzte Abgleichbudget dauerhaft verbrauchen.
- Neu: Kamera-Rig bleibt unverändert. Ein eigener starrer Kartenrahmen bildet gespeicherte Koordinaten auf die aktuelle Welt ab. CPU-Abfragen, Mesh/Collider und GPU-Integration verwenden denselben Rahmen. Bestehende V1-Profile bleiben lesbar; beim erneuten Speichern werden Anker, Boden und Schrein im Kartenrahmen abgelegt.
- Laden: Datei prüfen → Anker binden und stabile verfolgte Pose abwarten → Karte ausrichten/importieren → nur Oberflächenabgleich, keine zweite Rekonstruktion → ausdrückliche Freigabe. Fehler/Timeout/Abbruch räumen die temporäre Karte vollständig auf und kehren ins Menü zurück.
- Freigabe erst nach mehreren passenden Ansichten. Messfenster kann sich von frühen Fehlmessungen erholen. Gespeicherte Karte bleibt bis zur Freigabe für Spielpfade gesperrt. Danach werden gespeicherte Daten tatsächlich wiederverwendet und im Spiel weiter aktualisiert. Größere Möbeländerungen erfordern bewusstes Neuscannen; keine Behauptung, alte unbeobachtete Freiräume wären Live-Messungen.
- Recenter, Trackingverlust oder Fokus-Rückkehr dürfen nicht unbemerkt einen neuen Scan über eine gespeicherte Karte legen.

## Prüfstrategie

Nicht nur Identitätsmatrizen: verschobene/gedrehte Karten, abweichende neue Startposition, lokale Ankerpose nach wiederholtem Speichern, Mesh-/Abfrage-/GPU-Koordinatenkonsistenz; bestehende echte Nutzerdatei unverändert lesbar. Negativfälle: falscher Raum, frühe falsche Messungen, Timeout, Abbruch und Recenter. Vollständige Regression, APK-Prüfung. Getragener Kaltstart-Speichern-Laden-Zyklus bleibt erforderliche Hardwareabnahme, keine alleinige Freigabe durch synthetische Tests.

## Neuer Ablauf und Grenzen

1. **Neu scannen:** Startmenü → NEUEN RAUM SCANNEN → wie bisher ausreichend Umgebung erfassen und Scan bestätigen → Schrein platzieren → RAUM SPEICHERN anklicken. Der Vorgang speichert den räumlichen Meta-Anker und die checksummierte rekonstruierte Karte, keine Kamerabilder. Vorhandene Slots werden atomar ersetzt, die vorherige Datei bleibt lokal wiederherstellbar.
2. **Gespeicherten Raum verwenden:** Startmenü → RAUM … LADEN. Das System wartet nach dem Binden auf mindestens 600 ms stabile, tatsächlich verfolgte Ankerpose. Danach erscheint ausschließlich die goldene gespeicherte Karte. Der kurze Tiefenabgleich verwendet aktuelle Oberflächenpunkte, legt aber keine neuen Chunks an und baut keine zweite Geometrie auf.
3. Nach mindestens 48 passenden Punkten aus drei Richtungsbereichen und drei Kartenteilen bei mindestens 80 % Übereinstimmung erscheint **RAUM VERWENDEN**. Langsam verschiedene Wände ansehen; es wird kein lückenloser neuer Raumscan verlangt. Visuell prüfen, ob das Netz auf der realen Umgebung liegt, dann anklicken. Bei passender lokaler Spielfläche wird die gesamte gespeicherte Karte freigegeben. Andernfalls bleibt die abbrechbare Prüfung offen und bittet um einen Standort innerhalb der erfassten Fläche.
4. **ABBRECHEN** ist während Ankersuche und Kartenprüfung verfügbar. Ohne passende Messungen endet die Prüfung nach 35 Sekunden im Menü. Sobald die Karte erkannt ist, wartet die visuelle Bestätigung ohne Zeitdruck. Falsche frühe Messungen verfallen nach vier Sekunden, statt den Abgleich dauerhaft zu blockieren.
5. Erst nach Annahme verschwindet das goldene Referenznetz. Die vollständigen gespeicherten Kollisions-/Freiraumdaten werden genutzt; normale Live-Aktualisierungen laufen im Hintergrund wieder weiter. Der Schrein wird nur auf einer im Abgleich beobachteten passenden Auflage automatisch wiederhergestellt, sonst bewusst neu platziert.
6. Recenter, verlorener Anker oder Fokus-Rückkehr mit möglicher Neulokalisierung führen zur Raumwahl statt zu einem unsichtbar neu begonnenen Scan. Anker und Systemberechtigungen bleiben Voraussetzung. Nach Löschen von Quest-Raum-/Ankerdaten kann ein neuer Scan nötig sein. Verschobene Möbel oder neue Hindernisse außerhalb des aktuellen Sichtfeldes sind nicht sofort zuverlässig aktualisiert: dann neu scannen. Die System-Sicherheitsbegrenzung wird nicht verändert.

## Verbindliche Headset-Abnahme

- Zuerst den vorhandenen Raum laden, bewusst von anderer Startposition und mit anderer Blickrichtung. Goldene Wände/Boden dürfen nicht doppelt oder versetzt erscheinen. Während der Prüfung kein blaues zweites Netz; Freigabe und Abbruch jeweils erreichbar.
- Nach Freigabe sollen auch weiter entfernte gespeicherte Bereiche verfügbar sein, ohne sie vollständig neu abzulaufen. Unbekannte Bereiche bleiben unbekannt.
- Am Schrein erneut speichern, App vollständig beenden, erneut öffnen und laden. Zwei weitere Kaltstarts mit anderer Blickrichtung; kein über Zyklen wachsender Versatz.
- Laden abbrechen und direkt wieder laden. Im falschen Raum prüfen: keine unbemerkte Freigabe, sauberer Rückweg zu NEUEN RAUM SCANNEN.
- Während Prüfung und Spiel Quest-Menü öffnen/zurückkehren beziehungsweise Recenter auslösen: klarer Neustart der Raumwahl, keine halb gültige Karte.
- Reaktionszeit/Frametimes beim Import und Freigeben sowie Linkshänder-Pointer prüfen. Native Editor-/GPU-/ADB-Ergebnisse ersetzen diese getragenen Tests nicht.
