# Zusatzblock V19.3 – Wurfsterne in der freien Hand

**Historischer V19.3-Stand:** [V19.6](REVIEW-V19-6.md) ersetzt Hüftaufnahme und alte Wurfkurve durch handnahen Vorrat, Grip-Aufnahme ohne Körpergriff und leichte unterstützte Würfe. Ruhiges Loslassen gibt den Stern jetzt überall zurück. Die Waffenhand ist seit V19.4 wählbar.

Auftrag vom 7. September 2026: geliefertes Blender-Modell prüfen und drei regenerierende Wurfsterne als zusätzliche, bewegungsbasierte Waffe vor dem nächsten Spieltest integrieren.

## Umsetzung

1. **Assetprüfung:** FBX aus `Downloads/Wurfstern_Game_Export` in Blender geöffnet: ein Mesh, ein Material, 9.724 Quelldreiecke. Unity importiert 9.716 Dreiecke; Bounds 0,07757 × 0,01289 × 0,07757 m, also etwa elf Zentimeter diagonal. Kein Ersatz durch Primitive. Originaldateien im Download-Ordner unverändert; FBX-Kopie per SHA-256 identisch (`c9fff484dc144083101e4b265084e17b910c8393c5dd56a5e8b5dffe9f8931ba`).
2. **Material:** Albedo, Normal und AO aus dem gelieferten Export. Metall/Rauheit werden deterministisch in die vom vorhandenen SpatialPBR-Shader erwarteten Kanäle gepackt: R=Metall, A=1−Rauheit. Das gelieferte ORM ist kein direkt passendes Unity-Standard-Maskenlayout. Vier 1K-Maps mit Mips und Android-ASTC-6×6, ein gemeinsames tiefenverdecktes Material. Rohkarten außerhalb Resources; Laufzeit lädt keine Blender-Shader und keinen zusätzlichen GLB-Importer.
3. **Aufnahme:** Drei sichtbare Sterne am geschätzten linken Hüftpunkt; aktueller Revolver bleibt rechts. Freie Hand in ca. 20-cm-Greifzone bewegen, Grip drücken und halten. Kein zusätzlicher Trigger. Hüftrichtung hat eine Totzone und wird beim Annähern der Hand stabilisiert. Das ist eine Schätzung aus HMD-Pose, kein echtes Körpertracking. Langsames Loslassen direkt am Holster legt denselben Stern zurück.
4. **Wurf:** Arm-/Handbewegung aus kurzem Verlauf weltweiter Controllerpositionen; Grip am Bewegungsende loslassen. 1,35-fache Geschwindigkeitsunterstützung, maximal 12 m/s; keine automatische Zielsuche. Tracking-Sprünge löschen den Verlauf. Der Stern liegt mit seiner Ebene in der Vorwärtswurfrichtung und rotiert im Flug. Außerhalb des Holsters führt auch langsames Loslassen zu einem verbrauchten, fallenden Stern.
5. **Gameplay:** Drei Trefferpunkte Schaden (entspricht drei normalen Revolvertreffern), nur Monster. Raumflächen werden kontinuierlich entlang kleiner Flugabschnitte geprüft, einschließlich dünner Wände und Live-Depth-Raycasts; Monsterkontakt über ihre tatsächlich deformierte Oberfläche und Portal-Sichtgrenzen. Keine groben Körperbox-Treffer, kein zusätzlicher Fernschaden hinter Wänden. Sterne lösen keine Siegel, Relikte oder Schreinaktionen aus. Am Monster verbraucht, an Raumkontakt kurz sichtbar, danach entfernt; keine Rückgewinnung geworfener Sterne. Maximal sieben Sekunden freie Flugzeit.
6. **Vorrat:** Erst nach dem dritten tatsächlichen Wurf startet ein 150-Sekunden-Timer. Dann regenerieren alle drei zusammen. Ein oder zwei verbrauchte Sterne werden nicht einzeln aufgefüllt; Halten des letzten Sterns startet keinen Timer. Pausen frieren Timer und Flüge. Gehaltene Sterne werden bei Pause oder Trackingverlust zurückgelegt; danach frischer Grip-Druck erforderlich. Reset/Tod/Rescan entfernen zugehörige Flüge und setzen den Rundenvorrat zurück. Kein Verbrauch normaler Munition.
7. **Integration/Prüfung:** Neues Material/Modell vor Live-Scan vorbereiten. Ein gehaltener Stern sperrt die linke Zweihandstütze des Revolvers. Native Tests für Vorrat, erneutes Greifen, Rücklegen, Bewegung/Loslassen, Pause/Trackingverlust, Regeneration, echte Mesh-Treffer, dünne Wände, schwachen Fallwurf, Lebensdauer und Assetdarstellung. Bestehende funktionale Regression bleibt im vollständigen Build enthalten.

## Bedienung

Links zur Hüfte greifen → Grip halten → Hand nach vorn schleudern → **nur Grip loslassen, den Controller festhalten**. Trigger bleibt für diese Mechanik unbenutzt. Die Anzeige am Holster zeigt Vorrat oder verbleibende Sekunden. Nach kompletter Auffüllung gibt es eine kurze Rückmeldung.

## Offene Headset-Abnahme

Der Einbau ist implementiert: 2.160 native CHECK-Meldungen bestanden, darunter 51 Wurfstern-Checks. Android-Version 0.19.3/code42 ist gebaut, Metadaten und Signatur geprüft, aber mangels ADB-Verbindung noch nicht installiert; Lieferstatus im [Buildbericht](BUILD-REPORT.md). Noch zu prüfen: tatsächliche Hüfterreichbarkeit und Controller-/Sternpose, natürliches Timing und Wurfweite, Tracking bei seitlichem Ausholen, Textlesbarkeit und Quest-Frametime bei gleichzeitigen Kämpfen. Die native Ansicht ersetzt diese Abnahme nicht: [Unity-Modellansicht](../Verification/ThrowingStars/star-unity.png).

Keine Änderung des übrigen Hauptplans und keine Behauptung, dass echte Körper- oder Handverfolgung implementiert sei.
