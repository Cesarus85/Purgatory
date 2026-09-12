# A8 – Testplan für den platzierbaren Ritualschrein

Stand: 6. September 2026. Dieser Plan begrenzt A8 auf Platzierung, Bedienzustände
und die Integration in Start/Pause. Der bestehende Lauf `RearPortalValidation.Validate()`
bleibt die Regressionbasis (zuletzt 1.074 native `CHECK`-Meldungen). Diese Zahl ist
kein A8-Ergebnis und ersetzt keine getragene Quest-Abnahme.

## Audit vor der Umsetzung (V18.12)

- `QuestGun.Fire()` prüft zuerst `AudioMixButton`, danach nur
  `SpatialControlConsole`; erst danach greift die Pause-Sperre für normales
  Schießen. Schrein-Vorschau, Drehen, Bestätigen und Abbrechen müssen in diesem
  kontrollierten Strahlpfad vor der Gameplay-Sperre landen. Nur `IShotTarget` zu
  implementieren reicht nicht, solange der Gun-Pfad keinen allgemeinen Zieltyp
  dispatcht.
- `SpatialControlConsole.OnShot()` ruft direkt `QuestDemonGame.ToggleGameplay()`
  auf. A8-Bedienelemente dürfen diesen Übergang nicht indirekt auslösen:
  Vorschau/Rotation/Bestätigen/Abbrechen müssen von Start/Pause getrennte Aktionen
  sein und dürfen keine Munition verbrauchen.
- `ToggleGameplay()` setzt `Time.timeScale`, die Live-Scan-Vorschau, den HUD-Hinweis
  und den alten Console-Zustand. Ein Schrein-Ersatz muss dieselben Zustandswechsel
  nutzen, ohne eine zweite WaveLoop zu starten. Pause/Fortsetzen muss auch dann
  möglich bleiben, wenn der Schrein außerhalb des Blickfelds steht.
- `LiveRoomScanner.ResetMap()` verwirft bei Rescan/Recenter/Tracking-Sprung die
  Raumkarte und ruft `QuestDemonGame.OnLiveMapReset()` auf. Eine bestätigte
  Schreinpose darf danach nicht blind weiterverwendet werden: sie muss für die
  Sitzung invalidiert werden und vor dem nächsten Start neu bestätigt werden.
  `OnApplicationPause`/Resume und ein echter Recenter gehören deshalb in die
  Geräteabnahme.
- Die bisherige `EnsureLiveConsolePlacement()`-Logik prüft Boden, freie Körperfläche,
  Sichtsegment und Distanz nur für die alte Console. Für A8 muss dieselbe
  Sicherheitsrichtung gelten: erkannte Fläche, ausreichendes Fuß-/Körpervolumen,
  freie Sicht/Anmarschfläche; unbekannte oder durch Möbel belegte Bereiche sind kein
  gültiger Fallback.
- `AudioMixPanel` hängt aktuell an der Console, ist nur in Pause sichtbar und stoppt
  die Vorschau bei Start, App-Pause, Reset und Disable. Wenn der Schrein dieses Panel
  trägt, darf Umplatzieren keine zweite Instanz erzeugen und die laufende
  Klangvorschau nicht als verwaiste Audioquelle zurücklassen.
- Beschriftungen müssen den A7-Vertrag (`CompactText`, zentriert, begrenzte Breite,
  mehrere kurze Zeilen) fortführen. Die Schrift muss zur Bedienseite zeigen; eine
  Editor-TextMesh-Prüfung allein beweist aber nicht, dass sie im getragenen Headset
  aus beiden relevanten Blickwinkeln lesbar ist.

## Native Editor-Prüfungen

`Editor/ShrineValidation.cs` führt zuerst die bestehende Kette über
`RearPortalValidation.Validate()` aus und ergänzt danach die folgenden A8-Prüfungen.
Die Regeltests verwenden echte Unity-Collider für die Oberfläche und kontrollierte
`knownFree`-/Volumenfunktionen; sie simulieren keinen Tiefensensor.

| ID | Prüfung | Erwartung |
| --- | --- | --- |
| S01 | Neue `ShrinePlacementSession`, `Cancel()` vor erstem Confirm | Keine Pose wird bestätigt; kein Platzierungsmodus bleibt aktiv. |
| S02 | `Begin()` und ungültiger Kandidat, danach `Confirm()` | `Confirm()` liefert `false`; ein ungültiger Kandidat startet keine Runde. |
| S03 | gültige Pose setzen und bestätigen | `Confirmed=true`, `Placing=false`, Kandidat verbraucht keinen Spielzustand. |
| S04 | bestätigte Pose, erneut `Begin()`, andere gültige Pose, `Cancel()` | Vorherige Bestätigung und Pose werden wiederhergestellt; neuer Kandidat wird verworfen. |
| S05 | `Invalidate()` nach bestätigter Pose | Pose ist nicht mehr startberechtigt; erneut `Begin()`/Confirm erforderlich. |
| R01 | Breite ebene Boden-/Tischfläche, `yaw` 0/45/90° | `TryEvaluate()` akzeptiert die Pose; alle neun Auflageproben liegen auf gleicher geeigneter Fläche. |
| R02 | fehlender Treffer und nach unten gerichtete Normale | Beide Fälle werden mit nichtleerem Grund abgelehnt. |
| R03 | Fläche schmaler als der rotierte Footprint (`HalfWidth=.26`, `HalfDepth=.22`) | Keine Teilplatzierung; vollständige Breite/Tiefe ist Pflicht. |
| R04 | eine Ecke höhenversetzt | Unebene Auflage wird abgelehnt. |
| R05 | `knownFree` meldet unbekannt/falsch im Körpervolumen | Unbekannter oder belegter Körperraum wird abgelehnt. |
| R06 | `volumeClear` meldet belegtes Volumen | Besetztes Volumen wird abgelehnt, auch bei gültigem Oberflächentreffer. |
| R07 | horizontale Distanz zum Kopf `< .75 m` bzw. `> MaxDistance` (`4 m`) | Beide Distanzgrenzen werden abgelehnt. |
| R08 | Basis mehr als `1.05 m` über `floorY` | Erhöhte Tisch-/Ablagefläche wird abgelehnt. |
| R09 | gültige Fläche in mehreren Yaw-Lagen | Rotation beeinflusst die neun Stützproben und die zurückgegebene Pose; keine Achsenannahme nur für Yaw 0°. |
| R10 | Regelgründe | Jeder negative Pfad liefert einen verständlichen, nichtleeren `reason`; kein negativer Pfad wird als gültige Pose ausgegeben. |

Bis das Ritualschrein-Asset vorhanden ist, bleiben Mesh-, Material-, Collider- und
Artprüfungen aus dem Validator heraus. Das verhindert, dass ein fehlendes
`RitualShrineV18_13` einen reinen Regel-/Session-Lauf künstlich grün färbt oder
blockiert.

## Getragene Quest-Gates

Diese Punkte sind manuell mit demselben nicht-Development-Build, derselben
Systemlautstärke und einem realen Raumscan zu prüfen. Native Regeln, APK-Installation
oder ein synthetischer Raum ersetzen sie nicht.

1. **Startzustand:** Nach Kaltstart bleibt das Spiel pausiert; der Schrein ist in
   Vorschau, es gibt keinen automatischen Start und keinen Munitionsverlust.
2. **Pointer und Drehen:** Rechter Controllerstrahl findet Boden und geeignete
   Tischfläche; Yaw-Schritte sind sichtbar und bleiben bei Kopfbewegung lesbar.
   Schreinpose und Schriftseite stimmen bei frontalem und seitlichem Blick.
3. **Ungültige Flächen:** Wand, Sofa/Möbel, schmale Kante, unbekannte Scanfläche,
   zu nah und zu weit zeigen eine klare ungültige Anzeige. Trigger in diesem Zustand
   startet weder Runde noch Audio-Menü.
4. **Bestätigen/Abbrechen:** Gültige Position bestätigen startet erst auf der
   vorgesehenen Startaktion. Abbrechen stellt eine vorherige bestätigte Pose wieder
   her; beim allerersten Abbrechen gibt es keine startberechtigte Pose.
5. **Pause ohne Schreinblick:** Während des Spiels Pause über den vorgesehenen
   Controllerweg auslösen, Kopf vom Schrein wegdrehen und fortsetzen. Danach in Pause
   den Schrein erneut öffnen, umplatzieren und bestätigen; keine zweite WaveLoop.
6. **Tracking-Lebenszyklus:** Im pausierten Zustand Rescan (rechten Stick zwei
   Sekunden), Recenter sowie Headset absetzen/aufsetzen ausführen. Alte Weltkoordinaten
   dürfen nicht automatisch wieder starten; Scan muss bereit sein und die Pose erneut
   bestätigt werden.
7. **Raum- und Spielschutz:** Schrein blockiert weder reale Laufwege noch sichtbare
   Portalaustritte. Nach Confirm/Cancel/Reset existieren keine doppelten Schrein-
   Collider, Panels, Audioquellen oder Vorschau-Effekte.
8. **Audio-Menü:** Das bestehende Waffen-/Treffer-Menü ist nur in Pause erreichbar;
   Umplatzieren erzeugt keine zweite Instanz. Klangtest stoppt beim Start, Reset,
   App-Pause und Entfernen des Schreins.
9. **Sichtbarkeit und Komfort:** Text bleibt aus der realen Spielposition zentral,
   lesbar und zur Bedienseite ausgerichtet; keine Sicht- oder Tiefenverdeckung durch
   Glut, Schreinmodell oder Scan-Vorschau.
10. **Kurzprofil:** Nach wiederholtem Start/Pause/Umplatzieren/Rescan mindestens
    zehn Minuten auf Quest beobachten: keine wachsenden Objekte/Audioquellen,
    auffällige Frametimes oder Tracking-Sprünge. Das ist ein Kurzprofil, keine
    vollständige Langzeit-/Thermikfreigabe.

## Offene Gates vor A8-Freigabe

- getragene Controllerstrahl-/Triggerbedienung mit echter Flächenklassifikation;
- lesbare Schrift und korrekte Vorderseite bei realem Abstand, Kopfbewegung und
  beiden Yaw-Richtungen;
- Pause/Fortsetzen unabhängig vom Schreinstandort;
- Rescan, Recenter, Tracking-Sprung und App-Pause ohne Wiederverwendung alter
  Weltkoordinaten;
- tatsächliche Raum-/Portal-/Laufweg-Interaktion und Kurzprofil auf Quest 3;
- Quest-3S-/weitere Gerätematrix und vollständige Thermik-/Langzeitmessung bleiben
  separate Gates und werden nicht aus Editor-Checks abgeleitet.
