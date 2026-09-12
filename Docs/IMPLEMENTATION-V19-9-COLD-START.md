# V19.9 – Kaltstart der gespeicherten Raumkarte

## Erneuter Nutzerfehler und reale Evidenz

V19.8 ist **nicht physisch abgenommen**: laut Nutzer passt das erste Laden, nach App-Neustart ist das Netz wieder verschoben. Der am 7. September 2026 mitgelesene Prozess 30052 lädt Slot 1 mit 128 Chunks von 20:23:33 bis 20:23:38. Es entstehen tatsächlich keine neuen GPU-Chunks (gpu=0, unveränderte Revision=129). Um 20:24:13 meldet der bisherige Abgleich dennoch 48 passende Punkte; der Nutzer bestätigt gleichzeitig sichtbaren Versatz. Das belegt eine falsche Freigabe, nicht einen neuen parallelen Scan.

Die Datei auf der Quest ist weiter hashidentisch mit der vor V19.8 gesicherten Datei. Keine Löschung, automatische Konvertierung oder Überschreibung des Nutzerraums.

## Konkreter SDK-Befund

Lokales eingebundenes Meta-SDK, Package com.meta.xr.sdk.core@c0efcbf2ba70:

- OVRSpatialAnchor.TryGetPose / UpdateTransform verwenden weiterhin Camera.main und ToWorldSpacePose. Die Umrechnung enthält Kamera-Weltpose multipliziert mit inverser aktuell abgefragter Kopfpose.
- OVRLocatable.TrackingSpacePose (OVRAnchor/OVRAnchorComponents/OVRLocatable.cs, localToWorldPoseDeprecationMessage) warnt ausdrücklich: OVRTask kann vor OVRCameraRig.Update fertig sein; dadurch wird gegen eine veraltete Kamera lokalisiert. Empfohlen ist die explizite trackingSpaceToWorldSpaceTransform-Überladung.
- Die bisherigen Tests deckten korrekte starre Transformationen ab, nicht diese inkonsistenten SDK-/Kamera-Zeitpunkte. Der genaue physische Versatz ist im V19.8-Log mangels Poseaufzeichnung nicht quantitativ messbar. Daher bleibt die Zuordnung dieses konkreten Kaltstartfehlers zum SDK-Pfad eine begründete Ursache, noch keine Hardwarebestätigung.

## Umsetzung

- RoomTrackingAnchor besitzt den nativen OVRAnchor selbst. Erzeugung mit expliziter Trackingraum-Pose, Laden per UUID, aktiviertes OVRLocatable/OVRStorable, Lesen über TryGetSpatialAnchorPose plus OVRCameraRig.trackingSpace. Keine Camera.main-Umrechnung im maßgeblichen Speicher-/Ladeweg.
- Aufräumen auch nach spät abgeschlossenen, bereits abgebrochenen nativen Requests; Dispose betrifft die geladene Runtime-Instanz, löscht **nicht** den persistierten Anker.
- Stabile verfolgte Pose bleibt erforderlich. Nachträgliche stabile Lokalisierungskorrekturen während der Kartenprüfung richten den Kartenrahmen neu aus und verwerfen alte Prüfpunkte. Keine zweite TSDF-Rekonstruktion, keine Spielerbewegung. Nach Freigabe weiterhin sichere Rückkehr ins Menü bei großem Versatz/Ankerverlust.
- Geometrieprüfung benötigt zusätzlich tatsächlich aus dem gespeicherten TSDF abgeleitete Normalen: mindestens acht Bodenpunkte und je acht Punkte auf zwei nicht parallelen Wandrichtungen. Blickrichtungen allein zählen nicht als unabhängige Flächen. Acht-Sekunden-Messfenster; Menü sagt gezielt BODEN ANSEHEN bzw. ZWEI VERSCHIEDENE WÄNDE ANSEHEN.
- Speichern prüft, dass sich die Ankerpose während der Kartensicherung nicht um mehr als 2 cm / 0,5 Grad verändert hat. Andernfalls bleibt die bisherige Datei erhalten.
- Lokale begrenzte Diagnose in room-tracking/current.log und previous.log, maximal 1.000 Zeilen je Sitzung, auch nach App-Ende lesbar. Enthält Karten-/Anker-/Tracking-/Kopfposen und Status, keine Bilder; keine Übertragung an Dritte. Gerätedaten nur zur lokalen Fehleranalyse.

## Abnahme

Die automatischen Prüfungen müssen insbesondere die alte gemischte Kopfpose mit großem Versatz nachstellen, Unabhängigkeit der neuen Umrechnung von der Kamera zeigen, falsche Boden-/Parallelwand-Freigaben ablehnen und tatsächliche TSDF-Flächen nach erneuter Ausrichtung wieder bestätigen.

Danach auf der Quest **vorhandene unveränderte Karte** laden, visuell prüfen, App vollständig beenden und zweimal neu starten (andere Blickrichtung/Startposition). Pro Start Diagnose sichern. Erst nach diesen getragenen Versuchen gilt die Kaltstartzuordnung als bestätigt. Wenn weiterhin verschoben: Diagnose vergleichen, nicht pauschal neue Raumdaten verlangen oder als gelöst melden.
