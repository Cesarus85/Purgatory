# Purgatory V19.16 – Wurfspur, freie Hand, Kreaturdetails und Portal-Atmosphäre

## Umfang

1. Wurfsterne erhalten eine silberblaue, weich auslaufende Flugspur: maximal
   32 Stützpunkte, 0,12 Sekunden Historie, maximal 1,15 m gerader Abstand.
   Sie folgt der realen ballistischen Bahn, stoppt am tatsächlichen Kontakt,
   berücksichtigt Stereo-/Environment-Depth und pausiert mit dem Projektil.
   Ein geteilter, vorgewärmter Werkstoff; keine Partikelwolke pro Wurf.
2. Die Shotgun sperrt die freie Hand nur bei eingerastetem Pumpgriff. Außerhalb
   des Pumpbereichs sind die übrigen Sterne sichtbar und normal werfbar.
   Frischer Grip innerhalb von 16 cm am Pumphebel gewinnt gegenüber einem Stern.
   Ein gehaltener Stern kann mit gehaltenem Grip innerhalb von 12 cm an den
   Hebel übergeben werden: derselbe Stern kehrt in den Vorrat zurück, der
   Pumpgriff rastet ein. Kein Wurf, keine Munitionserzeugung. Loslassen gibt die
   Hand wieder frei. Pause, Trackingverlust und Handwechsel bleiben abgesichert.
3. Blender: selektive Verfeinerung von Kopf/oberer Hautgeometrie des bestehenden
   Dämons, 7.769 → 11.590 Quellvertices, 22.756 Dreiecke, unverändert 21 Bones
   und Timeline 1–770. Keine neue Kreatur, keine ersetzten Animationen. Alte FBX
   liegt in `Verification/Atmosphere/baseline-EmberfiendAnimatedV12.fbx`.
   Bestehende 2K-Texturen erhalten ASTC 4×4 statt 6×6, vierfache anisotrope
   Filterung und zurückhaltend schärfere Mip-Auswahl. Zusätzliche Poren-/Normal-
   und Rauheitsvariation nur auf Kreaturmaterialien, in der Entfernung
   gegen Flimmern ausgeblendet. Keine künstlich hochskalierte 4K-Textur.
4. Öffnende Portale erzeugen einen sanften 1,05-Sekunden-Helligkeitseinbruch
   mit kleinem Nachzittern direkt in der Meta-Passthrough-Schicht, nicht durch
   eine schwarze Scheibe vor den Augen. Maximal 0,115 Helligkeitskorrektur,
   vier Sekunden Sperrzeit, keine Verstärkung durch gleichzeitige Portale.
   Kein Aufblitzen über die Ausgangshelligkeit. Pause, Fokusverlust, Spielende
   und Deaktivierung stellen den vorherigen Stil wieder her. Bestehende
   benutzerdefinierte LUT-/Graustufenstile werden nicht überschrieben.
   Abschaltbar am Schrein unter **KLANG → PORTAL-DUNKEL: AN/AUS**; gespeichert.

## Reproduktion

- Blender: `BlenderSource/refine_demon_v1916.py`, native Datei
  `BlenderSource/RiftStalkerV19_16.blend`; Bericht `Verification/Atmosphere/blender-refinement.json`.
- Gezielter Unity-Test: `QuestDemonMR.Editor.AtmosphereValidation.Validate`.
- Gesamtlauf/APK: `bash Tools/build-v19.16.sh`.

## Quest-Abnahme

- Sterne vor hellen und dunklen Wänden werfen, auch seitlich: lesbarer kurzer
  Schweif, kein Verlauf durch Hindernisse; Pause stoppt ihn.
- Mit Shotgun in der Führungshand einen Stern greifen/werfen. Danach Pumphebel
  greifen, ziehen/vorschieben und wieder loslassen. Mit gehaltenem Stern zum
  Hebel wechseln: Stern kehrt in den Vorrat zurück. Rechts- und Linkshand testen.
- Dämonenkopf nahe ansehen, Mimik/Angriff/Austritt/Bluttreffer prüfen; keine
  abgelösten Augen/Zähne oder flimmernden Hautflächen. Mehrere Gegner plus
  Shotgun-Treffer auf Framerate prüfen: mehr Geometrie ist kein kostenloser Effekt.
- Portalöffnung beobachten: reales Bild wird kurz etwas dunkler und normalisiert
  sich; kein dauerhaft dunkler Scan/Schrein. Währenddessen pausieren oder
  Headset absetzen. Schalter am Schrein aus- und einschalten.

Keine Änderungen am Raumkartenformat oder an gespeicherten Inventar-/Handrollen.
Editor-Renderings und SDK-Stilprüfungen beweisen nicht die tatsächliche
Passthrough-Wirkung oder Framerate auf der getragenen Quest.

## Ergebnis vom 8. September 2026

- Finaler Lauf: `Verification/Atmosphere/unity-qdmr-v1916-export.2kJv7T.log`.
  38 erfolgreiche Testsuiten, 3.582 protokollierte CHECKs einschließlich
  verschachtelter Wiederholungen und Helligkeits-Kurvensamples. 240 neue
  Atmosphere-Prüfungen; die bisherigen 117 Presence-Prüfungen bestehen ebenfalls.
- Reale Inputpfade: Sternwurf während Shotgun, Übergabe ohne Verbrauch/Wurf,
  Pump-Vorrang, Loslassen, Pause und Trackingverlust bestanden. Wurfspur an
  realer Wand gestoppt, pausiert unverändert, verschwindet nach dem Kontakt.
- Meta-SDK-Stilprüfung: relative Helligkeit, Ausgangskontrast/-sättigung,
  Wiederherstellung, Kein-Stil-Modus, Sperrzeit und abschaltbarer Menüpunkt
  bestanden. Das ist keine auf der Quest aufgezeichnete Passthrough-Abnahme.
- Blender-Datei exportiert 22.756 Dreiecke; Unity importiert 22.750 nach dem
  Entfernen degenerierter Flächen. Gesichtseinsätze, alle bestehenden
  Animationen, Sparse-Wound-Skinning, Knie-/Portalaustritte und Treffer bestehen.
- Native Vorschauen: `Verification/Atmosphere/star-trail.png` und
  `Verification/Atmosphere/demon-detail.png`. Desktop-Kaltschuss-Test der
  Shotgun: 45,52 ms für die gesamte Salve einschließlich Trefferpipeline;
  keine Aussage über Quest-Framerate. Zusätzliche Geometrie deshalb ausdrücklich
  im nächsten Headset-Test mit mehreren Gegnern prüfen.
- Der erste Importlauf wurde kontrolliert beendet, weil eine globale
  Importregel auch SDK-Texturen invalidierte. Die endgültige Implementierung
  konfiguriert ausschließlich vier Kreaturtexturen explizit; der anschließende
  gezielte und der vollständige Prüflauf sind erfolgreich.
- Android/ARM64-Build erfolgreich, Gradle 2:46 Minuten. Neue Vulkan-Varianten
  von SpatialPBR und StarFlightTrail erfolgreich exportiert, keine C#-/Shaderfehler.
- APK: `Builds/Purgatory-v19.16-stars-atmosphere.apk`, 118.749.506 Bytes,
  Version 0.19.16 / Code 55 / Paket `de.stefanmaier.questdemonmr`.
  SHA-256: `3aca71bfc6c1ee30393b26c8f8abfee9d9c564c749987e7809693bdf04f61aa4`.
  ZIP-Integrität, Paketmetadaten und APK-v2-Signatur geprüft, gleiches Zertifikat
  wie V19.15. Vorgänger-APK per Prüfsumme unverändert. Nicht installiert.
