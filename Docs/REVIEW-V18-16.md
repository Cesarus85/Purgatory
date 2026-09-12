# V18.16 – Lavabrücke und alternative Gegnerauftritte

Stand: 6. September 2026. **Implementiert, nativ geprüft und als Release gebaut; noch nicht installiert oder am getragenen Headset abgenommen.** Nutzerfeedback zu V18.15: insgesamt besser, kontinuierlicher Austritt gut; Wunsch nach einem weiteren Ort, Sprüngen und kopfüber beschleunigtem Fledermausaustritt. Dies ist ein Teil von A10b, nicht der Abschluss des gesamten A10-Art-Plans.

## Inhalt

- **Zweiter eigener 3D-Ort aus Blender:** gebrochene Brücke über einer tiefer gelegenen Lavakluft, Felswände, Pfeiler und Ketten, ferner Torbau und gemeinsame Zitadellenmerkmale. Schmiede und Brücke wechseln bei aufeinanderfolgenden Portalerzeugungen ab. Gleiche Art-Richtung, tatsächlich andere Geometrie und eigener Atlas statt bloßer Perspektiv-/Farbvarianten. Beide erhalten einen einfachen animierten Rauchhimmel. Bestehender Stereo-Pro-Auge-Renderer, Portalrahmen und Rendertextur-Budgets bleiben erhalten.
- **Alternativer Bodenaustritt:** schlanke Bodendämonen springen gelegentlich über die Schwelle. Neue Blender-Animation mit Ausholen, Armen vor dem Körper, angezogenen Beinen und Landung; echte Bewegung entlang einer geprüften Kurve. Normales Herausschreiten und gelegentliches Umschauen bleiben erhalten. Brutes und Boden-/Deckenöffnungen verwenden nicht diesen Wandportal-Sprung.
- **Gelegentlicher Angriffssprung:** nach jeder dritten geeigneten Gelegenheit zwischen 2,1 und 3,8 Metern, mindestens neun Sekunden Pause pro Gegner. 0,38 Sekunden sichtbare Vorbereitung, 0,68 Sekunden Flug, 0,28 Sekunden Landungsphase; maximal 2,2 Meter Strecke und 0,42 Meter Kurvenhöhe. Ziel wird vor dem Absprung festgelegt, nicht in der Luft nachgeführt. Höchstens einmal zehn Schaden bei tatsächlicher Nähe und freier Sicht am Landekontakt. Keine Erhöhung von Lebenspunkten oder Waffenabschwächung.
- **Alternativer Fledermausaustritt:** beschleunigt kopfüber aus dem Portal, anfangs nach hinten gefaltete Flügel, anschließend Rolle um den Rumpf und Übergang in normalen Flug. Neue Blender-Flügelanimation; Körperrolle als Runtime-Transformation. Die bestehende U-Angriffsanimation bleibt separat und unverändert.

## Raum- und Kampfregeln

- Vor Beginn müssen Ziel, Flug-/Sprungkurve und relevante Körpervolumen im bekannten freien Raum liegen. Bei ungeeigneter Geometrie bleibt der normale Austritt. Die virtuelle Hälfte hinter der Portalfläche nutzt weiterhin die bestehende, vorher geprüfte Öffnung; daraus folgt keine Freigabe für beliebige reale Wände.
- Pfade werden in kleinen Abschnitten auch bei längeren Frames erneut geprüft. Angriffssprünge setzen einen begehbaren Boden unter der gesamten Strecke voraus: **kein neuer Sofasprung oder Möbel-Vault**. Unbekannter Raum bleibt verboten.
- Treffer in der Vorbereitung brechen den Angriff ab; in der Luft wird der Schaden deaktiviert, die Bewegung aber bis zur Auflage fortgesetzt. Taucht dort ein Hindernis auf, wird horizontal gebremst und nach unten auf eine gemessene Auflage gefallen. Der normale Raumreset entfernt weiterhin die Gegner. Ein vollständiges Verschwinden aller Bodenmessungen während des Falls ist kein separat bestandenes Hardware-Gate.
- Beim schnellen Portalübertritt neu blockierte Austritte werden abgebrochen und durch die bestehende Spawn-Wiederholung ersetzt. Die Quote wird nicht als besiegt gezählt. Gegner bleiben beim sichtbaren Austritt beschießbar; keine künstliche Unverwundbarkeit.
- Keine Änderung an Raumscan-Verfahren, Startphasen, Revolver, Audiopegeln oder Lebens-/Munitionsversorgung aus V18.15. Keine neuen Fremdassets.

## Art und Reproduktion

- `BlenderSource/build_bridge_v18_16.py` → `BrokenBridgeV18_16.blend`, FBX und eigener 2048²-Albedo/AO-Atlas. **14.676 Dreiecke, vier Asset-Materialslots**, ASTC 6×6; zusätzlicher Sky-Dome mit einem eigenen Material/Draw pro Portalauge. Vier Slots sind ausdrücklich nicht das Budget des gesamten Portals inklusive Rahmen und Effekten.
- `BlenderSource/build_arrivals_v18_16.py` → `RiftStalkerV18_16.blend`, `InfernalBatV18_16.blend`; stabile Runtime-FBX-Dateien um `Leap` bzw. `InvertedBurst` erweitert. Vorherige FBX-Kopien in `Verification/ArrivalVariants/baseline-*`, ursprüngliche Blender-Dateien erhalten. Keine neue externe Lizenzabhängigkeit.
- `Tools/build-v18.16.sh` führt Import, vollständige native Regression, Android-Export und `Tools/package-v18.16.sh` aus. Release-Artefakt: `Builds/QuestDemonMR-v18.16-arrival-variants.apk`.

## Tatsächlich geprüft

**1.371 native CHECK-Meldungen** im finalen Exportlauf, darunter **70 neue ArrivalVariantValidation-Prüfungen**, plus erweiterte Clip-/Assetfälle der bestehenden Kette. Keine Aussage über 1.371 unabhängige physische Tests.

Geprüft: beide importierten Orte/Atlanten, sechs aufeinanderfolgende Produktionsportale ohne unmittelbare Ortswiederholung, Clips, vollständige bekannte Kurve, unbekannte Ziele und dünnes Hindernis, verzögerter Absprung, Pause, echte Wurzelbewegung, festes Ziel trotz Spielerbewegung, echte Trefferunterbrechung, Spielerabstand, neues Hindernis während Flug mit Landung, beide schnellen Austritte, Rückfall auf normalen Austritt, animierte Renderkopie, exakte Endposition und zurückgesetzte Körperorientierung. Frühere Portal-/Combat-/Pickup-/Startup-Regressionen ebenfalls durchlaufen.

Native Bilder unter `Verification/ArrivalVariants/`: `forge.png`, `bridge.png`, `leap-windup.png`, `leap-air.png`, `leap-land.png`, `inverted-exit.png`, `inverted-roll.png`, `inverted-flight.png`. Bilder auf tatsächliche Pose und sichtbaren Übergang geprüft. Das sind Editor-Aufnahmen, keine Quest-Passthrough-/Stereo-Abnahme. Blender-Ansicht `bridge-blender.png` ist ebenfalls nur eine Art-Vorschau.

Ein früher Gesamtlauf fand unerwünschte PelvicCurl-Anteile im neuen Bat-Clip. Die Animation wurde korrigiert (neutraler Becken-Curl, Flügelfaltbewegung separat); die alte Regression wurde nicht abgeschwächt. Archivierte Fehllogs tragen `before-wing-fold`. Finaler Export erfolgreich; bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.

APK-Metadaten, ARM64, Release ohne Debuggable und APK-v2-Signatur mit einem Signer bestätigt. **0.18.16 / code 34, 99.633.022 Bytes**. SHA-256 `66da764e43e1158a7320720341b96ea27c31c140804687f122abaaf0c38687cf`. Vollständiger Nachweis in `Verification/ArrivalVariants/delivery-v18.16.txt` und [Buildbericht](BUILD-REPORT.md).

## Nächster Quest-Test / offene A10-Arbeit

1. Schmiede und Brücke aus verschiedenen Winkeln anschauen: Geometrie unterscheidbar, Stereo/Schärfe und Bildrate tatsächlich angenehm?
2. Mehrere Wellen in großem und engem Raum: erkennbare Sprungvorbereitung, angemessenes Tempo und Spielerabstand; erlaubtes Ausweichen auf normalen Austritt in engem Raum.
3. Dämon während Ausholen und Flug treffen; keine schwebende Trefferpose oder unverdienter Angriffsschaden. Neue reale Hindernisse bleiben ein Hardware-Test.
4. Fledermaus von vorne/seitlich beobachten: nachvollziehbare kopfüber Phase, Flügelöffnung und Rumpfrolle, danach normaler Flug und Beschießbarkeit.

**Kathedralen-Seitenhalle und ortsspezifisch modellierte Rahmen bleiben offen.** Die neue Brücke liefert einen zweiten Ort, keinen abgeschlossenen opulenten Gesamt-Art-Pass. Hintere Bodenportalverteilung sowie V17-Langzeit-/Thermiktests bleiben zurückgestellt. Installation und getragenes Headset-Feedback für V18.16 stehen aus.
