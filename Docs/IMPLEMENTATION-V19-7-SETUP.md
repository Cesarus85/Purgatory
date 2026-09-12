# V19.7 – verständliche Raumeinrichtung und Portal-Sprungprüfung

## Auftrag und Umsetzungsschritte

1. Ein einziges ruhiges Startpanel mit echten Ziel-/Abzug-Schaltflächen: gespeicherten Raum laden, weitere Räume wählen oder Scan starten. Keine technischen Dauerhinweise oder verdeckte Aktionsliste. Bestehende Links-/Rechtsrollen behalten.
2. Exklusive Anzeigen: Startmenü, Scanhinweis, Schreinplatzierung und Spiel-HUD nicht übereinanderlegen. Scanhinweis auf notwendige Aktion begrenzen; Diagnose nur ausdrücklich während geeigneter Phase.
3. Direkter `RAUM SPEICHERN`-Knopf am platzierten Schrein. Freier Platz wird automatisch gewählt; erneutes Speichern aktualisiert den aktiven Raum. Bei vollen Plätzen bewusstes Ersetzen mit Rückfrage. Datei und Meta-Anker weiterhin verifiziert; erkennbare Fortschritts-, Erfolgs- und Fehlerrückmeldung.
4. Portalsprung: gemeinsame Vorab-/Laufzeitprüfung, tatsächlichen unteren Rahmen und Übergang durch die gemessene Wand berücksichtigen. Keine pauschale Abschaltung realer Möbelkollision. Fehlgeschlagenen Austritt möglichst vor sichtbarer Bewegung erkennen; bereits sichtbare Gegner nicht abrupt löschen.
5. Native Ablauf-/Eingabe-/Textlayout-/Geometrieregression, gerenderte Menüansichten, vollständiger Androidexport, Signatur und APK prüfen. Reale Anker-/Kopf-/Controllertests separat ausweisen.

Keine vorhandenen Raumprofile löschen; normale APK-Aktualisierung erhält Gerätedaten. Kein neuer Hauptplanblock.

## Zusätzlicher Namensauftrag

Der sichtbare Spielname ist **Purgatory**: Android-Anzeigename, Startpanel und Schrein-Titel. Der technische Paketname `de.stefanmaier.questdemonmr` bleibt bestehen; kein zweites App-Paket und keine Änderung des Android-Datenverzeichnisses. Bisherige Dokumente und APKs bleiben historische Stände.

## Bedienung ab V19.7

1. Beim Start sieht der Spieler ein dunkles, kontrastreiches Purgatory-Panel mit abgegrenzten Schaltflächen. Mit der Waffenhand zielen und den Abzug kurz drücken. Alternativ Stick und die angezeigte primäre Taste verwenden. Kein Halte-Prozentzähler zum normalen Laden.
2. Ohne vorhandenes Profil direkt `NEUEN RAUM SCANNEN` wählen. Bei vorhandenen Profilen steht der zuletzt gewählte Raum zuerst; `ANDEREN RAUM LADEN` zeigt bis zu fünf Räume plus Zurück. Verwaltung/Entfernen bleibt eine separate Seite mit ausdrücklicher Rückfrage. Eingaben müssen nach Öffnen/Trackingverlust neutral sein; ein gehaltener Abzug bestätigt keine zweite Seite.
3. Während des Scans bleibt nur der kurze Scanhinweis sichtbar. Nach der Mindestfläche wie bisher die angezeigte Taste der freien Hand zwei Sekunden halten. Bei linker Waffenhand ist das A, bei rechter Waffenhand X.
4. Schrein platzieren: Waffenhand auf die gewünschte geeignete Fläche richten, Abzug kurz drücken. Währenddessen keine Lebens-, Wellen-, Munitions- oder Diagnosetexte über der Anleitung.
5. Am Schrein den eigenen Knopf `RAUM SPEICHERN` anvisieren und klicken. Ein freier Speicherplatz wird automatisch gewählt; beim aktiven gespeicherten Raum wird dieser aktualisiert. Nur bei fünf belegten Plätzen ohne aktiven Raum ist eine bewusst bestätigte Ersetzung nötig. Der vorherige Dateistand bleibt als `.previous`-Backup erhalten.
6. Während Anker/Datei gespeichert werden zeigt das einzelne Startpanel den Vorgang. Nach verifiziertem Schreiben und Lesen schließt es; der Schrein-Knopf zeigt fünf Sekunden `RAUM N GESPEICHERT`. Fehler sind kein Erfolg: Das Menü bleibt offen, der ausführliche Grund steht im Geräteprotokoll. Reale Anker-Speicherzeiten können nicht aus Editorprüfungen abgeleitet werden.
7. Beim nächsten App-Start `RAUM N LADEN` klicken. Die vollständige goldene gespeicherte Referenz bleibt erhalten; aktuelle Ausrichtung und lokaler Freiraum werden weiterhin durch Tiefendaten geprüft. Das Menü bleibt bei einer räumlichen Neuausrichtung vor dem Spieler. Es ist kein Laden ungeprüfter alter Freiräume.

## Sprungkorrektur und Grenzen

Der Rahmen selbst besitzt keinen eigenen Kollisionstreiber für Gegner. Ein Abbruch kann wie ein Anprall aussehen, wenn die Live-Geometrieprüfung den Austritt verwirft und die sichtbare Rückkehr startet. Ohne den betreffenden Lauf im Geräteprotokoll ist die konkrete Nutzerbeobachtung nicht abschließend zugeordnet.

Bodendämonen erhalten eine spezielle Austrittskurve: früheres Anheben, ungefähr 48 cm Wurzelhub über der Schwelle, späteres Absenken. Vorabprüfung und Laufzeit verwenden dieselbe Kurve; 80 statt 24 Vorabproben verringern ungeprüfte Zwischenstellen. Eine einzelne wechselnde Tiefenmessung stoppt zunächst die Bewegung, erst nach 0,3 Sekunden anhaltender Blockade beginnt die bekannte geprüfte Landung oder sichtbare Rückkehr. Möbel- und Freiraumprüfungen bleiben aktiv. Fledermäuse behalten den bisherigen unmittelbaren Sicherheitsabbruch ohne zusätzliche Wartezeit.

Echte geänderte Hindernisse können weiterhin einen sichtbaren Rückzug auslösen. Diese Fassung behauptet nicht, jeden bisher beobachteten Anprall ohne Headsetabnahme reproduziert zu haben.

## Headset-Abnahme

- Kaltstart ohne Textüberlagerung, beide Waffenhände, Ziel-/Abzugbedienung und versehentlich gehaltene Tasten.
- Frischen Scan abschließen, Schrein frei platzieren, einmal `RAUM SPEICHERN` klicken und Erfolg abwarten. App vollständig beenden, wieder starten und denselben Raum laden. Kein Menü-Umweg zum Speichern.
- Wiederholtes Speichern aktualisiert denselben Platz. Vorhandene andere Räume bleiben erhalten; volle Liste und Abbrechen der Ersetzungsfrage testen.
- Nach Platzierung kein Kopf-HUD vor den Schreinbuttons; nach Spielstart normale Kampfwerte, in Pause wieder Schreinbedienung.
- Mehrere Bodensprünge über den unteren Portalrand beobachten, auch an kleinen Portalen. Echte Hindernisse nicht absichtlich betreten oder verschieben. Fledermaus-Sturz und reguläre Wellenzählung als Regression prüfen.
- Physischer Save/Load-/Relokalisierungszyklus, reale Fuß-/Rahmenfreiheit, Lesbarkeit und Quest-Framezeiten bleiben separate Abnahme vom nativen Build.
