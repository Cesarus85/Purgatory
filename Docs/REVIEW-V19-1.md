# V19.1 – Kampfrhythmus und lebendigere Gegner

## Verhalten

- Siegel sind keine Pflichtaufgabe mehr. Ausgewählte Bodenportale zeigen zwei, ab Welle vier drei Ziele, sobald der erste Gegner eingetreten ist. Er bleibt aktiv; die Hauptwelle läuft weiter. Erfolgreiches Versiegeln verhindert nur den noch nicht begonnenen zweiten Austritt.
- Ohne Eingriff folgt nach ungefähr drei Sekunden der zweite Gegner, sobald Austritt und Crowd-Limit es erlauben. Normale Zweierportale warten nur 0,65 Sekunden. Ein blockierter Austritt verlängert die Siegelchance höchstens auf sechs Sekunden; danach verschwinden die Ziele auch ohne Schuss. Keine zusätzlichen Gegner außerhalb der endlichen Wellenquote.
- Maximal zwei gleichzeitig offene/schließende Portale, unverändertes raumabhängiges Gegnerlimit. Einzelerlebnisse und Fledermausportale haben keine Siegel. Ungeeignete Siegelpositionen deaktivieren nur das Extra, nicht den Portalplatz.
- Parallel laufender Nachschub zählt bis zum Abschluss zur Welle. Ein wirklich abgebrochener zweiter Austritt wird als einzelner unerfüllter Slot erneut versucht. Pause friert die Zeiten; Portalzerstörung bei Tod/Reset beendet die zugehörige Routine. Noch nicht zerstörte Siegel blockieren das Wellenende nicht.

## Modell und Animation

Der Dämon wurde tatsächlich in Blender bearbeitet: `BlenderSource/build_face_v19_1.py` erzeugt die editierbare `RiftStalkerV19_1.blend` und das ausgelieferte FBX. Drei zusätzliche Deform-Gelenke bewegen Unterkiefer und modellierte obere Lider. Untere Zahninseln folgen dem Kiefer, obere Zähne und Augen bleiben am Kopf. 21 Knochen, 7.769 Blender-Vertices, keine Ganzkörper-Blendshapes. Vorheriges FBX unter `Verification/RhythmLife/baseline-EmberfiendAnimatedV12.fbx` erhalten.

Nach der bestehenden Animation folgen kleine, geglättete Kopf-/Blickbewegungen, Atmung und Gewichtsverlagerung; Kiefer/Lider reagieren auf Angriff und Treffer. Kein neues Root-Movement. Blick und Mimik ersetzen kein vollständiges filmisches Gesichtsrig.

Die Fledermaus nutzt ihr vorhandenes deformierbares Modell: stabilisierter Kopf, Kiefer-/Ohrenreaktionen, kurze eingeblendete Gleitphasen aus einer bestehenden Flugpose, reduzierter Flügeltakt, asymmetrische Innenflügelfaltung in Kurven und verzögert nachführende Beine. Flügelschlag-Audio wird beim Gleiten ausgesetzt. Kein neu beschafftes Fledermausmodell; bestehende Krallenattacke, Portalsturz, Sprünge und Tod behalten Vorrang.

Additive Änderungen werden vor der nächsten Basispose entfernt und nicht aufaddiert. Sparsames exaktes Treffer-Skinning bleibt aktiv; neue Gesichts-/Flügelpositionen werden gegen das vollständig deformierte Unity-Mesh geprüft.

## Prüfung und offene Abnahme

Gezielte native Tests umfassen ignorierte und zerstörte Siegel über die Produktionsroutinen, tatsächliche Revolverschüsse, einmalige Munition, endliche Wellen-/Fledermausquoten, Nachschub-Abbruch/Retry, Pause und Accounting-Reset. Rig-Tests prüfen Driftfreiheit über 600 Schritte, Priorität von Angriff/Tod, Kopfgrenzen sowie alle deformierten Wundvertices gegen `BakeMesh` innerhalb drei Millimetern. Review-Bilder verwenden die tatsächliche gebackene Pose, um Unitys GPU-Skinning-Cache bei mehreren Editor-Aufnahmen im gleichen Frame zu umgehen.

Vollständiger Build-/Installationsstatus steht im [Buildbericht](BUILD-REPORT.md). Automatische Checks sind keine getragene Headset-Abnahme. Offen: Hitzigkeit und Verständlichkeit im echten Raum, wahrgenommene Natürlichkeit aus Spielabstand sowie Quest-Frametime/Temperatur bei zwei parallelen Portalen. Keine Behauptung, dass der gesamte V19-Ausbau abgeschlossen sei.

Der finale native Lauf bestand 1.936 CHECK-Meldungen, darunter 355 neue RhythmLife-Prüfungen und 25 erhaltene Portal-Art-Prüfungen. Bestehende Scan-, Knie-, Sturz-, Kampf-, Audio- und Reliktprüfungen sind enthalten. Gerenderte aktuelle Posen wurden angesehen: [Blick](../Verification/RhythmLife/demon-watch.png), [Knurren](../Verification/RhythmLife/demon-snarl.png), [Lidschluss](../Verification/RhythmLife/demon-blink.png), [Gleiten](../Verification/RhythmLife/bat-glide.png), [Angriff](../Verification/RhythmLife/bat-alert.png). Kein neuer Headset-Frametime-Nachweis.

Beim nächsten Test einmal sämtliche Siegel ignorieren: Gegner und Wellen müssen trotzdem weiterlaufen. Danach gezielt ein Nachschubportal schließen und auf weiterkämpfende erste Gegner achten. Gesicht aus normaler Spieldistanz, Gleit-/Kurvenwechsel und die erhaltene Kopfvoran-/Krallenattacke prüfen.
