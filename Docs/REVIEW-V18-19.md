# V18.19 – einfaches X-Halten, kniebetonter Schwellenübertritt, senkrechter Fledermaussturz

Stand: 6. September 2026. Korrekturauftrag nach Nutzertest V18.18. Der Nutzer bestätigt jetzt die vollständige Raumerfassung und deren positiven Einfluss aufs Spiel. **0.18.19 / code 37 um 19:19:33 datenerhaltend installiert**, Signatur, Geräteversion und APK-Hash bestätigt; nicht automatisch gestartet. [Buildbericht](BUILD-REPORT.md), [Liefernachweis](../Verification/KneeDive/delivery-v18.19.txt). Getragene V18.19-Abnahme noch offen.

## 1. Scan-Freigabe

- Nach stabiler gemessener Mindestfläche **X links zwei Sekunden halten**. Keine vorherige Loslass-/Neudrück-Geste mehr; bereits gehaltenes X zählt ab der Freigabe. Zeit vor ausreichender Messung zählt nicht.
- Kurze Anzeige mit Haltefortschritt; Loslassen, X+Y oder verlorene Mindestbereitschaft verwerfen den Teilfortschritt. Ein fortgesetzter Tastendruck bestätigt nur einmal. Diagnose bleibt bis zum Loslassen nach der Bestätigung gesperrt.
- Keine automatische Beendigung der Einrichtung. Die bestehende Mindestprüfung und 0,6 Sekunden Stabilisierung bleiben erhalten. Fortschrittsprozent ist ausschließlich die Haltezeit, kein behaupteter Scan-Abdeckungsgrad.
- Raumkarte, Tiefensensor, Streaminggrenzen, unbekannte Bereiche und Arbeitsbudgets unverändert. Nach Bestätigung läuft die Rekonstruktion weiter; es beginnt die Schreinplatzierung.

## 2. Knie statt Ferse anheben

Der bisherige 44-cm-Fußhub brachte den Knöchel teilweise über die Hüfte. Der vorwärts gerichtete Zweigelenk-Löser erzeugte dadurch einen rückwärts geknickten Unterschenkel. Beim Nachziehbein blieb der Fuß außerdem zu lange hinter der bereits vorgezogenen Hüfte.

Blender-`PortalStep` und raumabhängige Fußführung gemeinsam korrigiert: 35-cm-Schwellenhub mit angehobener Haltephase bis die gesamte Kralle frei ist, früheres Vorführen des Nachziehfußes nach dem Anheben und eine vorwärts/aufwärts gerichtete Kniehilfe. So hebt sich der Oberschenkel mit dem Knie, statt die Ferse nach hinten hochzuklappen. Reale Knochenpositionen, Hüftseite, Sohlen-/Krallenfreiheit und erreichbare Fußziele werden geprüft; Knochen werden nicht verlängert. Trefferflächen und entfernte Renderkopie verwenden weiterhin dieselbe Pose. Sprünge, Peek und Kampfclips bleiben erhalten.

Reproduzierbar mit `BlenderSource/build_knee_dive_v18_19.py`; Quelle `RiftStalkerV18_19.blend`, produktives `EmberfiendAnimatedV12.fbx`. V18.18-FBX unter `Verification/KneeDive/baseline-EmberfiendAnimatedV12.fbx` gesichert. Kein neues Fremdasset und keine Lizenzänderung.

## 3. Kopfvoran aus der Decke

Die bisherige 180-Grad-Längsrolle ist entfernt. Der alternative Auftritt fliegt zuerst **welt-senkrecht nach unten**, dann auf einer tangentenstetigen Kurve aus dem Sturz in den Anflug. Die tatsächliche Körper-zu-Kiefer-Achse richtet sich entlang der Flugrichtung aus – nicht nur ein abstraktes Transform-Up. Der bestehende Flügel-Faltclip bleibt erhalten; zum Schluss Übergang in die normale Flughaltung ohne Restrolle.

Vorabprüfung und Laufzeitbewegung verwenden dieselbe Kurve; bekannte freie Landung, Hindernisprüfung und Spielerabstände bleiben Pflicht. Für den Sturz wird zusätzliche Höhe unter dem Portal benötigt. Reicht der sichere Platz nicht, darf weiterhin normaler Flug gewählt werden, mit protokolliertem Fallback. Alternierende Auswahl aus V18.18 bleibt erhalten; kein Versprechen, dass jede Fledermaus stürzt.

## Prüfung und Abnahme

`KneeDiveValidation` verkettet die bisherige Regression und ergänzt Zwei-Sekunden-/Vorhalte-/Abbruchfälle, reale Knie-/Fußbeziehungen an drei Größen, echte Kieferausrichtung entlang der Kurve, senkrechten Anfang, kontrolliertes Ausleiten und ein Hindernis auf der Kurve bei freiem Endpunkt. Native Unity-Prüfbilder liegen unter `Verification/KneeDive/`.

**1.517 native CHECK-Meldungen bestanden, davon 52 neue KneeDive-Prüfungen.** Nachträgliches Hindernis bricht den Sturz ab und entfernt die entfernte Renderkopie. Reale Fuß-/Krallen-Skins wurden für alle drei Größen sowie schmale/kompakte Portale getestet. Erste Zwischenstände scheiterten an zu frühem Vorführen bzw. Absenken der Kralle; Vorhub und Haltephase wurden korrigiert, die Clearance-Grenze nicht herabgesetzt. Finale native Bilder für beide Knie und Sturz/Ausleiten visuell geprüft. Das ersetzt keine getragene Abnahme.

Im Headset noch prüfen:

1. Nach Mindestfreigabe X zwei Sekunden halten – auch bei schon vorher gedrückter Taste. Fortschritt muss verständlich sein, keine Diagnose nebenbei starten.
2. Vorderes und nachgezogenes Knie beim normalen Austritt beobachten; Füße müssen über den Rahmen gelangen. Auch kompakte Portale und Treffer während des Austritts prüfen.
3. Bei ausreichend Raum unter dem Deckenportal senkrechten Kopfvoran-Sturz und anschließendes Abfangen beobachten. Normale Flugvariante bleibt absichtlich enthalten.

Dies ist keine neue Langzeit-/Frametime- oder getragene Qualitätsabnahme. Restliche A10-Kathedrale und ortsbezogene Rahmen weiterhin offen.
