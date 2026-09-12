# V19.12 – Flüssiger Raumaufbau und lebendigere Präsentation

## Auftrag und Reihenfolge

1. Scan-Pipeline: Messdatenverlust im GPU-Pool verhindern, schwere Arbeit aus dem
   letzten Render-Callback nehmen, Ersterfassung beschleunigen, Mesh-/Physikkosten senken.
2. Schrein: Vorschau bei jedem Frame aktualisieren; teurere Freigabe getrennt und
   bei Bestätigung frisch prüfen. Keine ungeprüfte Position speichern.
3. Waffenhand: Meta-Controller unmittelbar vor dem Rendern aktualisieren, keine
   zusätzliche Positionsglättung; Links/Rechts, Kalibrierung und Rückstoß erhalten.
4. Monster: Blick, Gesicht, Gewichtsverlagerung und Flugreaktionen verfeinern;
   Übergänge, Pause, Austritt und Treffergeometrie nicht entkoppeln.
5. Siegel: kompakte, räumlich am Portal befestigte dämonische Sanduhr oberhalb
   des Durchgangs; echte Restzeit, Pause und vorzeitige Versiegelung beachten.
6. Automatisierte Regression, Lastfälle, native Renderkontrolle, Android-Paket.

## Bereits belegte Ursachen

- Schreinposition wurde nur bei vollständiger Prüfung im 100-ms-Takt geändert:
  sichtbar lediglich 10 neue Posen pro Sekunde, unabhängig von der Quest-Bildrate.
- GPU-Eviction schützte laufende Transfers, aber nicht noch gar nicht ausgelesene
  Integrationen. Bei mehr als 128 aktiven Abschnitten konnten Messungen verschwinden.
- GPU-Buffer-Erstellung und Upload liefen aus dem zeitkritischen BeforeRender-Pfad.
- Flächen wurden als unindizierte Dreieckslisten aufgebaut und beim Collider-Commit
  nochmals von PhysX bereinigt/verschweißt; Mesh-Erzeugung und Cook auf dem Hauptthread.
- Waffenmodell ist bereits direkt unter dem Controller verankert. Kein Modell-Lerp;
  Meta-Late-Controller-Update und Render-Last sind die relevanten Ansatzpunkte.

## Abnahmegrenzen

Keine Quest zum Arbeitsbeginn per ADB erreichbar. Desktop-/GPU-Prüfungen und
Build sind kein Nachweis für 72/90 FPS oder getragenes Tracking. Vorher/Nachher-Werte
für Scan, Schrein und echte Controllerbewegung benötigen einen Quest-Durchlauf.
Gespeicherte Räume und das A/B-Ladeverfahren dürfen nicht migriert/gelöscht werden.

## Umsetzung

- GPU-Daten werden erst nach tatsächlicher CPU-Übernahme parkbar (`RetainedAt`),
  nicht bereits nach Anforderung eines Readbacks. Fehlerhafte Transfers werden
  erneut eingeplant. Der 128er-Pool bleibt begrenzt; sichere Puffer werden umgehängt
  und mit den Daten des neuen Abschnitts initialisiert, statt neu allokiert.
- GPU-Residency/Uploads finden in Update statt. Die Tiefenbildentdeckung wird mit
  persistentem Puffer und 24/48/96 Strahlen je Frame abgearbeitet (Platzierung/Spiel/
  Erfassung). Neue Erfassung: maximal sechs kleine Integrationen statt zwei.
- Geometrie wird auf dem Worker indiziert. Testebene: 1.089 statt 6.144 Vertices
  bei denselben 2.048 Dreiecken. Keine vereinfachten Kollisionsflächen oder Löcher.
- Kollisionsmeshes werden mit exakt gleichen Cooking-Optionen auf einem Worker
  vorbereitet, dann auf dem Hauptthread angehängt. Maximal zwei Arbeitsketten.
  Reset/Destruction gibt ein noch laufendes Bake-Mesh erst nach Abschluss frei.
  Grundlage: [Unity Physics.BakeMesh](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.BakeMesh.html).
- Schrein: pro Frame ein Oberflächenstrahl, räumlich direkte Vorschau. Vollprüfung
  alle 80 ms und bei jedem Bestätigungsdruck. Vorschaubewegung allein autorisiert
  keinen Standort. „Raum speichern“-Text wird nicht mehr jeden Frame neu gesetzt.
- Waffe: Eingabeverarbeitung nach dem Rig-Update; Meta-LateControllerUpdate explizit
  aktiv, FixedUpdate-Tracking aus. Direktes Parenting bleibt ohne Glättung erhalten.
  Fallback aktualisiert nur gültiges Tracking auch vor dem Rendern, im Trackingraum.
  Das Meta-Prefab hatte LateControllerUpdate bereits standardmäßig eingeschaltet;
  Haupthebel gegen wahrgenommenen Nachlauf ist daher auch die reduzierte Frame-Last.
- Monster: archetypabhängige Atemfrequenz, asymmetrische Oberkörper-/Arm-Nachbewegung,
  variierende Blickbewegungen, Kiefer und Lider; Ohren, Flügelfalten und Beine reagieren
  bei Fledermäusen auf Blick, Kurven und Sinkflug. Geerdete Gangzyklen/Schritte bleiben
  distanzgetrieben. Zwei additive Schlagvarianten und festgelegte Schlagrichtung nach
  der Ausholphase; keine nach hinten homenden Treffer. Keine zusätzliche Root-Teleportation.
  Bestehende Blender-Rigs werden im Spiel ergänzt; keine neuen Monster-FBX in diesem Block.
- Siegel: kleines, räumliches Bronze-Sanduhrgehäuse mit körnigem Glutsand oberhalb
  des skalierten Portals, Restzeit auf Zehntelsekunden. Timer nutzt ausschließlich die
  bestehende Encounter-Zeit; Pause, Versiegelung, Timeout und Aufräumen bleiben gekoppelt.
  Keine Collider, zusätzlichen Portal-Kameras oder Partikelsimulation dafür.

## Gezielter Quest-Vergleich nach Update

1. Neuen Raum erfassen: breite Wand und Boden ohne mehrfaches Zurückschwenken;
   anschließend Möbel, seitliche Bereiche und Rückkehr zu bereits erfassten Stellen.
2. Schrein langsam und schnell über Boden/Tisch bewegen; Vorschau soll kontinuierlich
   folgen. Abzug auf ungeeigneter Fläche muss weiterhin abgelehnt werden.
3. Vorhandenen A/B-Raum laden; speichern/neu laden, Links-/Rechtshandwechsel und
   Abbruch/Neuplatzierung dürfen keine verschobenen Räume oder alten Standorte aktivieren.
4. Controller seitlich bewegen/drehen; dann schießen. Griff, Mündung, Rückstoß und
   Treffer müssen zusammenpassen – im Setup, Spiel und nach Pause.
5. Monster im Stillstand, Lauf, Kurve und Angriff ansehen. Trefferflächen müssen auf
   der bewegten Haut bleiben; Tod, Portal-Austritt und Pause dürfen keine Zusatzpose erhalten.
6. Sanduhr über Siegel-Portal ansehen, Pause testen, Siegel zerstören oder Zeit ablaufen
   lassen. Anzeige muss verschwinden, ohne den Gegnernachschub zu blockieren.

## Prüfstand

- Vollständiger Exportlauf: **3.020 erfolgreiche Prüfschritte**, einschließlich
  Mesh-/Compute-GPU, Raumprofilen, Scan, Schrein, Portal-Austritten, Kampf, Animation,
  Wurfsternen, Links-/Rechtshandwahl und Sanduhr. Log:
  `Verification/Fluidity/unity-qdmr-v1912-export.AWqUL9.log`.
- Nach Export ausschließlich Editor-Tests verschärft/aufgeräumt (kein geänderter
  Runtime-Stand): weitere **404 erfolgreiche Prüfungen** in
  `Verification/Fluidity/final-focused.log`. GPU-Donor enthält absichtlich alte,
  nichtleere Werte; Wiederverwendung muss diese vollständig zurücksetzen.
- Explizite Freigabe der GPU-Puffer in Edit-Mode-Testfixtures ergänzt. Dort läuft
  MonoBehaviour-OnDestroy ohne aktiviertes Spielobjekt nicht verlässlich. Die
  bekannten 320 Editor-Testallokationen treten im gezielten Abschlusslauf nicht auf.
  Dies ersetzt kein Langzeit-Speicherprofil auf der Quest.
- Native Physik-Testebene: 6.144 → 1.089 Vertices (**82,3 % weniger**), gleiche
  2.048 Dreiecke und Fläche. Desktop-Anhängen des vorgebackenen Colliders etwa
  0,3–0,9 ms in den protokollierten Läufen; kein Quest-Framerate-Nachweis.
- Monster-/Sanduhr-Speziallauf: 523 Prüfungen in `Verification/Fluidity/life-hourglass.log`.
  Deformierte Wundpunkte stimmen weiterhin innerhalb von 3 mm mit Unitys gebackener
  Skin-Geometrie überein. Zusatzposen akkumulieren nicht und überschreiben keinen Tod.
- Native Renderkontrolle: `Verification/Fluidity/hourglass-{full,empty}.png` sowie
  `Verification/RhythmLife/{demon-snarl,demon-watch,demon-blink,bat-glide,bat-alert}.png`.
- Die lokalen echten Raumprofile 2/3 behalten ihre ursprünglichen SHA-256-Werte:
  `440e757a005e5b01febe6ccfd90bbd162210522ee529440e73b98230c717417e` bzw.
  `8ab1fdc5666e60ea58f5ca399779d4ce8cf83f9836ef3791ad46f57169250140`.
- Unity-Export erfolgreich nach `/private/tmp/qdmr-v1912-export.AWqUL9`;
  Version `0.19.12`, Android-Code `51`. Compiler/Shader ohne Fehler. Bestehende
  Editor-TypeDB-/SDK-Warnungen sind kein bestandener Quest-Lauf.
- Keine neue Installation und kein getragener Quest-Performance-Test in diesem Schritt.

## Übergabepaket

- `Builds/QuestDemonMR-v19.12-fluidity.apk`, 106.659.720 Byte.
- Paket `de.stefanmaier.questdemonmr`, `0.19.12` / Code `51`.
- Gradle `BUILD SUCCESSFUL in 5m 25s`, ZIP-Prüfung fehlerfrei, Signatur gültig.
- APK SHA-256: `66aab80d70ee3ad90dbb85324b82661c6abd4739996c1774c4531e98a0d80807`.
- ARM64-lib in APK identisch zum frisch kompilierten `libil2cpp.so`:
  `55ea3d7a87332be6358868ebe88c17d776c1cd08dadab303df5bed6c001d6180`.
- Entwicklerzertifikat identisch zu V19.11:
  `3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2`.
- Abschließendes `adb devices -l`: kein Gerät. Als Update installieren, nicht
  vorher deinstallieren; vorhandene Raumprofile und Einstellungen behalten.
