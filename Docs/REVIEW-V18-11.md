# V18.11 – Raumnutzung, ehrliche Suchhinweise und kleinerer Revolver

Nutzerkorrektur nach A7, vor A8. **V18.11 / 0.18.11 / Code 29 ist gebaut, signaturgeprüft und am 6. September auf Quest 3 installiert.** Geräteversion und APK-Hash bestätigt; keine Daten gelöscht, nicht automatisch gestartet. Neue getragene Abnahme offen. [Build- und Installationsnachweis](BUILD-REPORT.md).

## Befunde

- Die Live-Platzierung blockierte die vier zuletzt verwendeten Stellen im Radius von 0,9 m vollständig. Erst nach zwei erfolglosen Suchdurchgängen wurde Wiederverwendung erlaubt. Ein begrenzter Raum konnte deshalb wiederholt vier Sekunden warten, obwohl ein physisch freier Austritt existierte.
- Jeder einzelne erfolglose Suchversuch schrieb sofort „KEIN FREIER AUSTRITT“. Beim nächsten erfolgreichen Versuch wurde dieser Hinweis nicht gelöscht. Das erklärt das mögliche gleichzeitige Auftauchen von Warnung und Portal; es ist kein Beleg, dass alle gemeldeten Raumprobleme nur Textfehler waren.
- 28 zufällige Suchrichtungen garantierten keine gleichmäßige Raumabdeckung. Die Bewertung bevorzugte einen festen Entfernungsbereich und seitliche Blickwinkel. Der genau auf einer Seite des Spielers liegende Weg-Endpunkt konnte außerdem durch ein Möbelstück belegt sein, obwohl daneben ein gültiger Anmarsch möglich war.
- Der aktuelle Revolver war bereits auf 90 % der Blender-Originalgröße gesetzt. Die Nutzerwahrnehmung im Headset ist maßgeblich; die alte Größenprüfung war keine Ergonomieabnahme.

## Änderungen

- 48 systematische Richtungen in zwei Höhen, vollständige 360°-Abdeckung und zwischen Suchen versetztes Raster. Seitliche Nachbarstellen einer gefundenen Wand werden in 30-/60-cm-Schritten ebenfalls geprüft; kein ungeprüftes Verschieben durch Möbel oder Wände.
- Kürzlich verwendete Stellen sind im Live-Modus kein Ausschluss mehr. Acht vergangene Positionen dienen einer räumlichen Wiederholungsbewertung: andere Bereiche und andere Richtungen bevorzugen, gegenüberliegende Wände nicht wie dieselbe Wand behandeln. Einzige sichere Austritte bleiben wiederverwendbar.
- Physisch passende Kandidaten deduplizieren, sortieren und an höchstens sechs Kandidaten vollständige Wegsuche durchführen. Vorher einen tatsächlich begehbaren und mit dem Spielerbereich verbundenen Zielpunkt aus mehreren Ringpositionen wählen. Bestehende Portalflächen-, bekannter-Freiraum-, Gegnerabstands- und 1,45-m-Spielerabstandsprüfungen bleiben erhalten.
- Deckenportale werden ebenfalls unter mehreren geprüften Möglichkeiten nach räumlicher Abwechslung gewählt, statt den ersten Treffer zu nehmen. Bekannte freie Austrittsbahn und Belegungsprüfung bleiben Pflicht.
- Eine kurze Suchlücke zeigt keine Fehlermeldung mehr. Erst nach sechs Sekunden ohne verbleibende lebende Gegner kommt „SUCHE PORTALPLATZ …“, höchstens alle zehn Sekunden. Erfolgreiche Platzierung löscht ihren Hinweis sofort, nicht andere Spielmeldungen. Die ausstehende Gegnerquote bleibt erhalten; kein leerer Wellensieg.
- Revolver **zusätzlich 20 % kleiner** als V18.10: 0,90 → 0,72 gegenüber Blender-Original. Griffpunkt am Controller bleibt exakt gleich; Mechanikteile und Lauf-/Rauchsocket werden gemeinsam skaliert. Schuss-/Trefferklang und Munition unverändert.

## Validierung und Grenzen

**954 CHECK-Meldungen im finalen nativen Validierungs-/Exportlauf, davon 59 neue Prüfungen.** Die vollständige bisherige Regressionskette ist erneut durchgelaufen. Neue Tests verwenden einen ausdrücklich synthetischen bekannten TSDF-Raum mit echten Unity-Kollidern, Boden, vier Wänden und Sofa. Der produktive Selektor findet bei 16 aufeinanderfolgenden Aufrufen jeweils einen Austritt, verwendet alle vier Wände und mindestens acht unterschiedliche 70-cm-Rasterzellen. Dabei wird die echte Navigation verwendet, keine vorgefertigte Ergebnisliste. In diesem Szenario genügt pro Aufruf eine Wegsuche.

Anschließend bleibt nur ein 1,4 m breiter Wandstreifen übrig: sechs weitere aufeinanderfolgende Platzierungen erfolgreich, auch nahe zuletzt verwendeten Stellen. Nach Entfernen aller bekannten Freiraumproben wird trotz vorhandener Kollisionsgeometrie kein Spawn mehr erlaubt. Zusätzlich 360°-Raster, verzögerte/ratenbegrenzte Meldung, gezieltes Löschen des Suchhinweises sowie neuer Waffenmaßstab, unveränderter Griff und vorwärts gerichtetes Laufsocket geprüft. Native Waffenansichten angesehen und unter `Verification/RoomUse/` gesichert.

Rohdaten: `Verification/RoomUse/synthetic-placements.csv`; Logs `quick-1.log`, `unity-export.log`, `build-driver.log`. Der auf der Quest verfügbare begrenzte Logcat-Ausschnitt enthielt keine früheren Platzierungsereignisse; die Ursachenbewertung beruht daher auf Code und reproduzierten Szenarien, nicht auf einem nachträglich behaupteten Mitschnitt der Nutzerrunde.

Das ist keine Behauptung gleicher Raumqualität wie andere Quest-Spiele. Scanlücken, reale Möbel, unvollständig erfasste Wände und enger Spielerabstand können weiterhin Platzierungen verhindern. Eine physische Abnahme von Verteilung, Wartezeiten, Größe und Frametimes bleibt nötig. A8 (platzierbarer Ritualschrein) wird hier nicht vorgezogen.
