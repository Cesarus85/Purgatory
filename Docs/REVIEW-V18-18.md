# V18.18 – Portalschwelle, erreichbarer Kopfüber-Auftritt und bewusster Scan-Abschluss

Stand: 6. September 2026. Freigegebener Korrekturblock nach V18.17-Nutzertest. Verbindlicher finaler Build-/Installationsstatus steht im [Buildbericht](BUILD-REPORT.md) und unter `Verification/ThresholdSetup/delivery-v18.18.txt`.

**Installiert am 6. September um 18:38:59: 0.18.18 / code 36.** 1.465 native CHECK-Meldungen, davon 58 neue Prüfungen; Release-Signatur, Geräteversion und installierter APK-Hash bestätigt. App-Daten und Berechtigungen erhalten, nicht automatisch gestartet. Getragene Abnahme weiterhin offen.

## Heraussteigen über die Portalkante

Der erhöhte Rahmen bleibt erhalten; keine neue bodentiefe Öffnung und keine gelockerte Wand-/Bodenprüfung. Der normale Lauf nutzt eine überarbeitete Blender-`PortalStep`-Animation mit kurzen Vorbereitungsschritten, zwei deutlich angehobenen Schwellenübertritten und anschließendem Stand im Raum. Normale Dauer weiterhin 1,15 Sekunden, seltenes Umschauen zwei Sekunden; funktionierende Sprünge unverändert.

Die beiden Fußziele folgen dem tatsächlichen Start-/Austrittsabstand und der Größe des Dämons. Ein begrenzter Zweigelenk-Löser passt Hüfte, Knie und Fuß an diese Ziele an, ohne Knochen zu verlängern. Beine werden nach ihrer tatsächlichen Hüftseite zugeordnet, nicht nach einer Annahme über gespiegelte Blender-/Unity-L/R-Achsen. Der Vorderfuß setzt weiter jenseits des Rahmens auf, der Hinterfuß hebt früh genug an, damit auch Krallen und Sohlen Abstand halten. Die hohe Fußhebung beträgt in dieser Bewegung bis 44 cm; die endgültige Pose wird im importierten Modell überprüft.

Der normale Akteur beginnt jetzt 62 statt 85 cm hinter der Portalfläche. Der Endpunkt bleibt der räumlich geprüfte Austritt. Beim Umschauen stehen beide Füße hinter der Schwelle; der Peek-Sampler verwendet die korrekten neuen Zeitgrenzen. Manuell gesampelte Clips werden deterministisch ohne Restgewicht des vorherigen Clips dargestellt. Treffer pausieren den Übertritt, erhalten aber die Schwellen-Fußziele; Tod/Cleanup und echte Beschießbarkeit bleiben Teil der bestehenden Regression. Keine zusätzliche Spielfigur, keine Laufzeit-Mesh-Bakes für die Fußkorrektur: Darstellung, Trefferfläche und entfernte Renderkopie verwenden dieselben Knochen.

Quelle/Reproduktion: `BlenderSource/build_threshold_v18_18.py`, `BlenderSource/RiftStalkerV18_18.blend`, stabile Runtime-FBX `Resources/Models/EmberfiendAnimatedV12.fbx`. Vorheriges FBX unter `Verification/ThresholdSetup/baseline-EmberfiendAnimatedV12.fbx`; bisherige Leap-/Combat-/Peek-Clips und Rig-/Meshgewichte erhalten. Keine neuen externen Assets oder Lizenzen.

## Kopfüber-Fledermaus: Auswahlfehler behoben

Der echte Fehler lag vor der Animation: Deckenplätze verlangten `(Welle + Index) % 4 == 1`, der Kopfüber-Auftritt anschließend einen geraden Wert. Diese Kombination war unmöglich. Ein eigener Zähler zählt jetzt nur Fledermaus-Auftritte und versucht beim ersten, dritten usw. den `InvertedBurst`; dazwischen normalen Flug. Bodenauftritte beeinflussen den Zähler nicht, ein neuer Durchlauf setzt ihn zurück.

Gemeinsame Decken-Eignungsfunktion wird in beiden Platzierungswegen verwendet. Die neue Prüfung läuft über diese tatsächliche Eignung, den produktiven Auswahlaufruf und bis zum initialisierten, wirklich kopfüber stehenden Akteur. Die bisherigen Freiraum-/Spielerabstandsprüfungen bleiben Pflicht: Ungeeignete Flugkurven können weiterhin auf normalen Flug zurückfallen; Grund wird protokolliert. Keine Zusage, dass jede Fledermaus kopfüber erscheint. Schacht, Flügelclip und Sprungvarianten aus V18.17 bleiben enthalten.

## Scan: X bestätigt die Einrichtung, nicht das Ende der Tiefenmessung

Der automatische Wechsel nach kurzer Mindestbereitschaft entfällt. Die Scanansicht bleibt standardmäßig sichtbar, bis der Nutzer nach Freigabe **X am linken Controller loslässt und anschließend eine Sekunde hält**. Kurzes Drücken, eine vorher gehaltene Taste oder X+Y bestätigen nicht. Verlorene Mindestbereitschaft verwirft eine teilweise gehaltene Bestätigung; nach Rückkehr ist ein neuer Tastendruck nötig.

Freigabe verlangt aktuelle Sensordaten, erkannte Boden-/Grundbereitschaft und mindestens drei benachbarte begehbare Messbereiche mit bekannten freien Verbindungen. Diese Mindestlage muss 0,6 Sekunden stabil sein, bevor die Anzeige zum Halten von X auffordert. Sie ist ausdrücklich **kein Prozentsatz des gesamten Raums** und garantiert nicht, dass bereits jede Wand oder genügend Portalplätze erfasst wurden. Es darf beliebig weiter geschaut/gescannt werden, bevor X bestätigt wird.

Nach Bestätigung beginnt die bestehende Schreinplatzierung und das sichtbare Gitter wird ausgeblendet. Die Live-Rekonstruktion läuft weiter, die Karte wird nicht gelöscht oder zu frei erklärt. Linker Stick kann die Vorschau weiterhin bewusst umschalten. Neuer Scan/Rezentrierung/Relokalisierung verwirft die Bestätigung; nach normalem Rundenende in derselben gültigen Raumsitzung ist kein unnötiger neuer Scan nötig.

X gehört während der Einrichtung ausschließlich dieser Bestätigung. Diagnose-X und X+Y sind dort gesperrt; nach dem Wechsel wird erst nach vollständigem Loslassen wieder auf Diagnoseeingaben reagiert. Trigger, A und Y dürfen die Einrichtung nicht umgehen oder dabei Munition verbrauchen.

**Nicht geändert:** Scan-Reichweite, 768 gespeicherte/128 GPU-residente Blöcke, Schutz benutzter Bereiche, Arbeitsbudgets und konservativer Umgang mit Unbekanntem aus V18.17. Ein zusätzlicher realer Sensor-/Rekonstruktionsstillstand im beanstandeten Test ist mangels erhaltenem Laufprotokoll nicht bewiesen oder pauschal als behoben ausgegeben. Dieser Block behebt den vorzeitigen automatischen Übergang und macht die Nutzerentscheidung eindeutig.

## Nachweise und offene Geräteprüfung

`ThresholdSetupValidation` verkettet die gesamte bisherige native Regression. Neue Fälle: frischer/kurzer/langer X-Druck, X+Y, verlorene Bereitschaft, kein automatischer Wechsel trotz längerer Bereitschaft, produktive Bestätigung/Platzierung, erhaltene Karte und nicht suspendierter Scanner, unverändert unbekannte Bereiche, Reset; vollständiger Fledermaus-Auswahlweg; tatsächliche Fußziele und Skin-/Krallenhöhe beim Übertritt für mehrere Größen und Portalformen, sowie Treffer während des Schritts. Frühere Erwartungen an Auto-Platzierung, Startabstand und Zeitpunkt der Fußhebung wurden auf die neuen Verträge geändert, die zugrundeliegenden Sicherheits-/Poseprüfungen erhalten.

Editor-Sichtprüfungen: `Verification/ThresholdSetup/step-front.png` und `step-rear.png`. Zusätzliche Prüfungen des sichtbaren Skins fanden Überlappungen, die reine Knöchelhöhen übersehen hatten; Aufsetzpunkt und Hubkurve wurden nachgebessert. Zwischenstände/Fehllogs bleiben erhalten. Dies ist kein Nachweis aller möglichen Frame-Mesh-Kontakte oder getragener Bewegungsqualität.

Nach Installation im Headset prüfen:

1. Nach Mindestfreigabe weiter umschauen: kein automatischer Wechsel. X loslassen, dann eine Sekunde halten → Schreinplatzierung; keine gleichzeitige Diagnoseaktion.
2. Normalen und kompakten Bodenaustritt seitlich ansehen: Fuß hebt vor der Kante ab und landet dahinter, Hinterfuß folgt. Auch während eines Treffers beobachten.
3. Ab den ersten möglichen Deckenportalen auf echten Kopfüber-Auftritt achten. Bei Ausbleiben `QDMR_ARRIVAL_FALLBACK` und Platzierungslogs prüfen, nicht nur den Animationsclip isoliert testen.
4. Nach Bestätigung weiter bewegen/umschauen: neue Messungen/Kartenrevisionen und korrekte Hindernisbeachtung kontrollieren; Framezeiten und längere Sessions weiter offen.
