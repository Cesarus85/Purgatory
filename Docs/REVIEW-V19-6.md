# V19.6 – Raumwahl, Portalaustritt und Wurfkomfort

Stand: 7. September 2026. Korrekturblock zum Nutzerfeedback; kein neuer Hauptplanblock.

## Raumwahl und gespeicherte Referenz

- Die alte Angabe `1/1` zählte Menüaktionen, nicht gespeicherte Räume. Jetzt wird die tatsächliche Zahl vorhandener Profildateien angezeigt; ihre Integrität wird beim Laden geprüft. Vorhandene Räume stehen zuerst, auch ohne letzte-Raum-Einstellung. `NEU SCANNEN / KEIN PROFIL LADEN` ist eine eigene, unmissverständliche Aktion.
- Während der Auswahl wird keine neue Tiefenrekonstruktion begonnen oder übernommen. Ein Speichervorgang meldet erst nach erneutem Lesen und Prüfen der geschriebenen Datei Erfolg. Fehler und Importfortschritt sind sichtbar.
- Nach dem Laden bleibt eine vollständige goldene Referenz der gespeicherten Oberflächen erhalten, unabhängig von der schrittweisen aktuellen Rekonstruktion. Diese Referenz besitzt keinen Collider und gibt keinen Freiraum frei. Unbestätigte alte Oberflächen werden nicht als aktuelle grüne Geometrie dargestellt.
- Das ist weiterhin Laden plus aktueller Ausrichtungs-/Freiraumabgleich, kein blindes Vertrauen in alte Möbelpositionen. Der bestehende Meta-Anker und die lokalen Freiraumprüfungen bleiben erforderlich. Die Netzanzeige lässt sich mit dem Stick-Klick der freien Hand umschalten.
- Wichtig: Scan mit zwei Sekunden der angezeigten Taste abschließen ist **nicht** Speichern. Danach im Raumprofilmenü am Schrein einen Speicherplatz wählen und die Erfolgsmeldung abwarten. Beim nächsten Start ausdrücklich den gespeicherten Raum laden. Ohne angeschlossene Quest kann nicht festgestellt werden, ob das im Nutzertest bezeichnete Profil tatsächlich auf dem Gerät gespeichert wurde.

## Sichtbare Erholung beim Portalsprung

Die bisherigen Sicherheitsabbrüche bei zu nahem Spieler, geänderter Sprungfreigängigkeit oder länger blockiertem Austritt konnten einen sichtbaren Gegner sofort ausblenden. Jetzt versucht ein bereits im Raum befindlicher Bodengegner eine kontinuierliche Landung auf geprüftem Boden. Andernfalls zieht er sich sichtbar entlang der bereits zurückgelegten Bahn ins Portal zurück. Erst dort wird der fehlgeschlagene Austritt abgerechnet. Ein erfolgreich gelandeter Gegner bleibt lebendig; keine neue Belohnung oder künstlicher Kill.

Die Rückbewegung prüft reale Raumcollider. Ist auch sie neu blockiert, bleibt der Gegner sichtbar stehen, statt durch Möbel zu gleiten. Eine freie Rückkehr ist dadurch nicht garantiert. Der bestehende Abbruch bei tatsächlich entferntem Portal und echte Schusstode bleiben unverändert. Getragene Tests müssen bestätigen, ob damit alle vom Nutzer beobachteten Fälle erfasst sind.

## Wurfsterne: freie Hand statt geschätzter Hüfte

- Drei kompakte Vorratssterne nahe dem Handgelenk der freien Hand, ungefähr zehn Zentimeter vom Controlleranker; kein Griff zu einer nur geschätzten Körperposition.
- Grip der freien Hand halten stellt einen Stern bereit, unabhängig von der Hüftposition. Direkt am Revolver bleibt Grip für die Zweihandstütze reserviert.
- Eine kurze bewusste Handbewegung und Grip loslassen werfen. Etwa fünf Zentimeter Bewegung reichen im synthetischen Test; jüngster Impuls bleibt bei leicht verspätetem Loslassen erhalten. Startgeschwindigkeit wird auf 5,5–12 m/s unterstützt, Richtung bleibt aus der tatsächlichen Bewegung, kein Homing.
- Mindestens drei Zentimeter gemessene Bewegung und eine Mindestgeschwindigkeit verhindern Würfe durch kleines Zittern. Ruhiges Loslassen gibt den Stern zurück, ohne Verbrauch. Reine Controllerrotation ist weiterhin keine Wurfbewegung.
- Drei Sterne, gemeinsame 150-Sekunden-Regeneration nach dem letzten Wurf, Pause-/Tracking-/Handwechsel-Schutz und bestehende Treffer-/Raumkollision bleiben erhalten.

## Gezielte Headset-Abnahme

1. Raum speichern und Erfolgsmeldung lesen. App vollständig beenden. Aus anderer Position erneut starten, gespeicherten Raum laden: korrekte Anzahl, klarer Ladefortschritt, vollständige goldene Referenz, kein versteckter Neuscan im Auswahlmenü. Aktuelle grüne Flächen dürfen sich ergänzen. Verschobene Möbel dürfen nicht als alter Freiraum gelten.
2. Mehrere springende Gegner beobachten, auch wenn der Spieler näher kommt. Kein abruptes Ausblenden mitten im Raum; kontrollierte Landung oder sichtbarer Rückzug. Nach Landung beschießbar, Wellenzählung läuft weiter. Nicht absichtlich an reale Hindernisse herantreten.
3. Beide Waffenhände prüfen: Vorrat an freier Hand, Grip ohne Körpergriff, Zweihandstütze am Revolver weiterhin möglich. Ruhiges Loslassen verbraucht nichts.
4. Mit kleinen, entspannten Bewegungen werfen, Richtung/Weite beurteilen. Drei Würfe leeren den Vorrat, nach 150 Sekunden aktiver Spielzeit kommen drei zurück. Pause und Handwechsel dürfen keinen unbeabsichtigten Wurf auslösen.

Native Testzahlen, APK-Hash und Installationsstatus stehen im [Buildbericht](BUILD-REPORT.md). Synthetische Editorprüfungen ersetzen keine getragenen Meta-Anker-, Komfort- oder Performanceprüfungen.
