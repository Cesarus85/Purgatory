# Quest Demon MR – Review V15

Stand: 5. September 2026. Erster Quellcode-, Gameplay- und Rendering-Durchgang nach dem Modellwechsel.

## Umgesetzte Änderungen

### Treffer und Fledermaus

- Schüsse schneiden die tatsächlichen Dreiecke der animierten oder starren Modelloberfläche. Die groben Körper-/Knochenkollider entscheiden nicht mehr, ob ein Gegner getroffen wird. Wände und schießbare Projektile begrenzen weiterhin die Schussdistanz.
- Die Skalierung gebackener SkinnedMesh-Geometrie ist korrigiert. Beim Bat führte die Kombination aus Importskalierung und `BakeMesh(false)` zu einer 16-fachen Vergrößerung der Treffergeometrie gegenüber dem gerenderten Modell. Beim Bodenmonster gab es ebenfalls eine Abweichung. `BakeMesh(true)` wurde hier gegen die direkte Berechnung aus Bindeposen und Knochenmatrizen geprüft, nicht nur nach Augenschein eingestellt.
- Wundmarkierungen verwenden Ausschnitte der getroffenen Dreiecke und folgen deren animierten Vertices. Kein frei schwebendes Quad, keine Annäherung über den nächstgelegenen Knochen. Der Shader berücksichtigt auch die reale Umgebungstiefe.
- Kein MeshCollider-Neukochen pro Schuss. CPU-Geometrie wird bei Bedarf je Frame zwischengespeichert.
- Die Fledermaus startet nicht mehr ständig Fly/Attack neu. Ein expliziter Sturzangriffszustand hat 0,48 s Vorwarnung, 1,05 s Angriff und anschließende Erholung. Auch nach einem langen Frame endet der Angriff korrekt.
- Vertikale Ausweichrichtungen ergänzen die horizontale Flugsteuerung. Blockierte Flugrichtungen wechseln nicht mehr bei jeder Steuerungsprobe ihre Ausweichseite.
- Der Bat ist jetzt richtig herum ausgerichtet; bisher lag sein Kopf entgegen der tatsächlichen Flugrichtung. Originale Farb-, Detail- und Normaltexturen sind explizit als Runtime-Assets gebunden statt als untexturierte Oberfläche zu erscheinen.
- Decken-Spawns prüfen den Ausflugskorridor gegen Raum und Möbel; auch während des Ausflugs wird die tatsächliche Bewegung geprüft.

### Raum und Bewegung

- Sichtverdeckung ist nicht gleich Hindernis: Der Live-Raumcache markiert die tatsächlich beobachtete Möbelfläche, nicht automatisch den verdeckten Boden dahinter. Sonst verschwanden potenziell freie Umwege um Sofas aus der Wegsuche.
- Die geglättete Bewegungsrichtung wird vor jedem Bodenschritt nochmals geprüft. Zwei sichere Richtungen ergeben beim Mischen nicht zwangsläufig einen sicheren Weg um eine Ecke.
- Separation berechnet Abstoßung einmal pro lebendem Bodenmonster, nicht mehrfach über dessen zahlreiche Ragdoll-Kollider. Tote Körper und Fluggegner blockieren die Boden-Crowdsteuerung nicht mehr.

### Gameplay und Bedienung

- Gleichzeitige Gegnerzahl abhängig von lokal freier Raumfläche: 2–5, im Fallback 3. Die Wellen bleiben erhalten.
- Nach einer geschafften Welle gibt es 12 Schuss. Bei vollständig leerem Magazin und Vorrat gibt es nach fünf aktiven Spielsekunden eine Notladung von vier Schuss, inklusive Nachladen.
- Start/Pause am Pult funktioniert ohne Munition und kostet keinen Schuss.
- Pause hält nun die Simulationszeit einschließlich Animationen, Angriffszeiten, Projektilen und Effekten an. Controllerbedienung und Pultinteraktion bleiben möglich. App-Unterbrechung pausiert das laufende Spiel.
- Neustart beendet die alte Wellenroutine und entfernt alte Portale, Projektile und Pickups.
- 0,55 s Schadensschutz nach einem Treffer verhindert gestapelte Sofortschäden mehrerer Gegner.
- Meldungen bleiben 2,6 s sichtbar, statt beim nächsten normalen HUD-Update sofort zu verschwinden. Kritisches Leben wird farblich hervorgehoben.
- Pickups von Fluggegnern werden auf Bodenhöhe platziert.

### Grafik und Portale

- Eigener `QuestDemonMR/SpatialPBR`-Shader verbindet Standard-PBR mit Metas Environment-Depth-Verdeckung. Der zuvor verwendete Meta-Beispielshader bot keine Normal-, Metalltextur- oder Emissionskarten. Diese Details gingen bei der Laufzeit-Materialkonvertierung verloren.
- Normal Maps, Metallic/Smoothness, AO, Emissionskarten und HDR-Emission sowie Textur-UV-Einstellungen werden erhalten. Die Waffe erhält eine weniger spiegelnde Metallabstimmung.
- Neue Portale entfernen den Höllen-Layer nicht mehr aus den Kameras bereits bestehender Portale.
- Die Blickpositionen der Varianten liegen vor der Landschaft; zwei frühere Positionen konnten innerhalb großer Felsen liegen und die Öffnung vollständig verdecken. Der Rahmen ist nun dunkler Obsidian/Knochen statt ganzflächig selbstleuchtendem Violett.
- Vier stärker getrennte Weltstimmungen: Gluthölle, grüne Seuchenwelt, violetter Riss und bernsteinfarbene Aschewelt. Lava/Leuchtflächen, Distanznebel und Silhouettenkanten verwenden zusammengehörige Farben. Bestehende dreidimensionale Landschaft und unterschiedliche Blickpositionen bleiben erhalten.
- Portal-Kameras rendern nur bei sichtbarer Öffnung im erweiterten Blickfeld. Die Schließverzögerung wurde auf 1,15 s verlängert.
- Häufige Partikelmaterialien werden wiederverwendet.

### Blender-Korrektur der Waffe nach der Sichtprüfung

Der erste Unity-Render zeigte eine schwer beschädigte Waffenoberfläche. In Blender wurde die Ursache nachgewiesen: Der ursprüngliche invertierte `Solidify`-Umriss war nach der Skalierung des Meshes weiterhin 0,05 Einheiten dick – nun fünf Zentimeter statt einer feinen Außenkontur. Dadurch entstanden massive Selbstdurchdringungen.

- Umrissmodifier entfernt; die eigentliche detaillierte Challenger-Geometrie und ihre UVs/PBR-Texturen bleiben erhalten.
- Die um diese aufgeblähte Hülle herum platzierten, frei schwebenden Zusatzblöcke entfernt.
- Hinteren Schaft verkürzt, Proportionen und Griffposition für eine einhändige Waffe angepasst; die tatsächliche Mündung nach vorne ausgerichtet.
- Mündungspunkt direkt in Blender als `MuzzleSocket` gesetzt. Schussursprung und Mündungslicht folgen damit auch dem Rückstoß des sichtbaren Laufs.
- Reproduzierbar über `BlenderSource/review_gun_v15.py`; editierbare Quelle `BlenderSource/HellcasterChallengerV15.blend`, Runtime-Modell `Models/HellcasterPistolV15.fbx`. Die alte V9-Quelle bleibt erhalten.

Es wurden vorhandene Blender-/externe Modelle weiterverwendet und korrigiert, keine neuen externen Assets gekauft oder heruntergeladen. Die Waffe wurde tatsächlich in Blender bearbeitet, nicht nur im Unity-Material umgefärbt.

## Reproduzierbare Prüfungen

Editor-Einstieg: `QuestDemonMR.Editor.CombatReviewValidation.ValidateAndBuild`.

28 gezielte Assertions prüfen unter anderem:

- Strahlen gegen rotierte und nicht einheitlich skalierte Meshes; Vorder- und Rückseite dünner Flügel; Begrenzung durch eine nähere Wand; keine Treffer in leerer Bounding Box.
- Wund-Geometrie unmittelbar auf der Oberfläche.
- Lesbare reale Bat-/Emberfiend-Meshes, sechs Trefferpfade pro Modell und Übereinstimmung der gebackenen Oberfläche mit Knochenmatrizen unter 1 mm. Gemessener maximaler Fehler im Log auf sechs Nachkommastellen: 0,000000 m.
- Deformation der Fledermaus zwischen zwei Fluganimationsphasen.
- Erhaltene Normal Map und HDR-Emission im MR-Material.
- Vorhandene Bat-Texturen, reparierte Waffenabmessungen und ein Mündungspunkt am vorderen Ende der Geometrie.
- Ende eines abgelaufenen Sturzangriffs, Unterscheidung von verdecktem Boden und Sofa, Freigabe veralteter Hinderniszellen.
- Bedienung des Pults ohne Munition sowie Pause/Resume des Simulationszeitmaßstabs.

Die Unity-6.3-Dokumentation bezeichnet `useScale` als Kompensation der Renderer-Skalierung. Entscheidend für diesen Fix ist zusätzlich der lokale Geometrietest mit den konkreten importierten Rigs: [Unity BakeMesh API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html).

Build- und Signaturstatus des ausgelieferten APK stehen in `BUILD-REPORT.md`.

Zusätzlich wurden sieben Bilder mit Unity auf dem Mac gerendert und visuell kontrolliert: `Previews/V15/bat.png`, `emberfiend.png`, `pistol.png` und `portal-0.png` bis `portal-3.png`. Einstieg: `QuestDemonMR.Editor.VisualReviewCapture.PrepareAndCapture`. Das sind echte Unity-Renderings mit Produktionsmodellen/-materialien, jedoch keine Quest-Passthrough-Aufnahmen. Sie zeigen noch keine vollständige Effektanimation und ersetzen keinen Stereo-Headset-Test.

## Noch am getragenen Headset zu prüfen

1. Körper und Flügel aus unterschiedlichen Winkeln treffen, auch während Flug und Ausflug außerhalb des anfänglichen Blickfelds. Keine abgesetzten Wunden.
2. Sofaecke und schmale Passage: von beiden Seiten spielen, auch wenn der Weg zeitweilig hinter Möbeln verschwindet. Die konkrete Raumgeometrie ist nicht durch Editor-Tests ersetzbar.
3. Zwei gleichzeitig sichtbare Portale, seitliche Kopfbewegung, Deckenportal und beide Augen. Kein schwarzer Portalinhalt; Stereo-Parallaxe und reale Verdeckung prüfen.
4. Pause während Sturzangriff, Feuerballflug und Nachladen; Headset kurz absetzen; kontrolliert fortsetzen. Neustart nach Tod beginnt bei Welle 1.
5. Vorrat vollständig leeren, Notladung abwarten, Pult mit leerer Waffe bedienen.
6. GPU-/CPU-Frametiming, Speicher und Thermik in einer mindestens 15-minütigen Sitzung. Keine Behauptung stabiler Quest-Bildrate allein aus dem Editor oder dem APK-Build.

## Nächster Qualitätshebel nach dem Test

Eine zusammenhängende Encounter-Runde mit klar unterscheidbaren Gegnerrollen, gezielter Geräuschvorwarnung und Belohnung für gutes Zielen; dazu ein Profiler-Durchgang auf Quest. Für einen weiteren sichtbaren Art-Sprung anschließend vor allem Charakter-Silhouetten, Übergangsanimationen und Portal-Nahgeometrie bearbeiten. Noch mehr Partikel allein lösen weder die Lesbarkeit noch die Bewegungsqualität.
