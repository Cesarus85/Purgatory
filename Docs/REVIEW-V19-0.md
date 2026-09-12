# V19.0 — Kathedrale, ortsbezogene Rahmen und Portalversiegelung

Stand: 6. September 2026. **0.19.0 / code 39 um 20:48:28 auf Quest 3 installiert**, Signatur und Geräteversion/Hash bestätigt; Daten/Berechtigungen erhalten, nicht gestartet. Umsetzung des empfohlenen nächsten Blocks; **V19 wird damit begonnen, nicht vollständig abgeschlossen**. [Arbeitsplan](V19-PORTAL-SEALING.md), [Buildbericht](BUILD-REPORT.md), [Liefernachweis](../Verification/PortalSealing/delivery-v19.0.txt).

## Neue Portal-Art

- Originale Blender-Kathedralenhalle: 28.350 Dreiecke, vier Materialien, eigene 2048er Farbatlas-Textur mit gebackener Umgebungsverdeckung. Freie Nahschwelle, gebündelte Säulen, Seitenkapellen, sich kreuzende Gewölberippen und ferne Ritualnische mit Rosettenfenster. Bis zu 24 langsam treibende gekrümmte Seelenpartikel, keine weiteren Echtzeitlichter dafür.
- Eigene Blender-Rahmen mit geschmiedeten Klammern/Nieten, unterbrochenen Ketten bzw. gotischem Maßwerk. 27.100 / 28.900 / 27.932 Dreiecke, jeweils drei Rendergruppen. Bewusst dieselbe obsidianartige Grundform und dieselben geprüften Abmessungen; Unterschiede liegen in plastischen Metallreliefs, nicht nur Farben.
- Bodenfolge zuverlässig **Schmiede → Brücke → Kathedrale**. Deckenrisse verbrauchen diese Folge nicht und behalten ihren separaten, aufrechten Fledermausschacht. Stereo-/Off-Axis-Renderer und Auflösungen unverändert, keine begehbaren Portale.
- Blender-Quellen und FBX liegen im Projekt; keine neuen Fremdassets oder Lizenzen. [Blender-Bild](../Verification/PortalSealing/cathedral-blender.png), [Unity-Kathedrale mit Siegeln](../Verification/PortalSealing/locale-3.png), [Schmiede](../Verification/PortalSealing/locale-0.png), [Brücke](../Verification/PortalSealing/locale-1.png).

## Neues Begegnungsziel

1. Ein Bodenriss schickt bis zu zwei Gegner, ein Deckenriss einen. Die bisherige gesamte Wellenquote bleibt erhalten. Gruppen überschreiben keinen vorgesehenen Deckenslot. Zweiter Gegner erst bei freiem Austritt und freiem Crowd-Platz.
2. Nach abgeschlossenem Nachschub folgt ein viersekündiges Angriffsfenster. Noch lebende Gegner greifen weiterhin an. Anschließend erscheinen **zwei Siegel**, ab Welle vier **drei**. Jedes benötigt einen Treffer.
3. Pro Freigabe werden einmalig zwei bzw. drei Patronen zur Reserve hinzugefügt, bis zur bestehenden Obergrenze. Die Notladung bei vollständig leerer Munition bleibt erhalten. Es gibt weder endlose Gegner noch eine automatische Siegelbelohnung.
4. Die originalen Blender-Siegel besitzen plastisches Metallfiligran und einen facettierten Kern. Treffer deaktiviert den Collider sofort, bricht die Modellteile auseinander und lässt sie verschwinden. Kein aufgeklebtes Einschussloch auf dem Trigger.
5. Erst das letzte zerstörte Siegel schließt den Riss. Beide Portal-Augenansichten werden vor dem nächsten Riss freigegeben. Die Welle endet erst nach Begegnungsabschluss und allen verbliebenen Gegnern; danach weiterhin +12 Munition.

## Räumliche und technische Grenzen

- Siegel stehen innerhalb der Öffnung vor der realen Wand/Decke. Freiraum und Sicht vom aktuellen sicheren Spielerstandpunkt werden vor Platzierung geprüft. Die Auswahl prüft Alternativen, statt bei einem schlecht sichtbaren besten Kandidaten endlos hängen zu bleiben. Reale Hindernisse fangen Waffenschüsse weiter ab; keine Freigabe unbekannter Räume.
- Das ist eine zusätzliche Bedingung an einen spielbaren Riss. Die Verteilung in sehr engen/teilweise erfassten Zimmern muss erneut getragen beurteilt werden. Keine Garantie, dass bei jeder späteren Spielerposition oder neu bewegten Möbeln dieselbe Sicht erhalten bleibt; nicht durch Möbel oder gefährliche Bereiche ausweichen.
- Fehlgeschlagene Austritte schließen ihren Riss ohne Bonus und wiederholen nur die noch offene Gegnerquote. Pause friert Zustand und Treffer ein; Tod/Neustart beendet Begegnungen. Siegelmaterialien einschließlich Tiefenverdeckungs-Kopien werden explizit freigegeben, auch wenn noch nicht aktiviert.
- V18.20-Scan, Zwei-Sekunden-X, Revolvergröße und Waffenklang sowie Knie-/Fledermauskorrekturen bleiben erhalten. Die Portaloberflächen-Materialkopien werden nun ebenfalls beim Abbau freigegeben.

## Prüfung und offene Abnahme

Neue `PortalSealingValidation`: endliche Quoten, separate Deckenslots, 2/3 Siegel, Zeit-/Pausengate, ungültige und doppelte Treffer, terminaler Abbruch/Abschluss; importierte Geometrie/Budgets/Atlas/Schwelle; echte Boden-Ortsfolge und senkrechter Schacht; gerenderte Unity-Ansichten; echte Spawn-Routine mit zwei Eintritten, einmalige Zusatzmunition, Warten ohne Wellenfortschritt, echte Gun.Fire-Schüsse mit realem Hindernis, Pause, Patronenverbrauch und Riss-Schließroutine. Materialfreigabe-Callback im Edit-Modus explizit aufgerufen, nicht als getragener Laufzeitnachweis dargestellt.

**1.737 native CHECK-Meldungen bestanden, davon 181 neue PortalSealing-Fälle**; gesamte V18.20-Regressionskette enthalten. Sechs zusätzliche Ressourcen erhöhen auch die bestehende Warmup-Prüfanzahl. Keine Behauptung eines getragenen Performance- oder Bewegungsnachweises.

Physisch offen: beide Augen/Parallaxe, Seelenbewegung, erreichbare Siegel am Boden und an der Decke, zwei Gegner am engen Austritt, Verständlichkeit ohne Erklärung, Rundenrhythmus, dynamisch versperrte Sicht und reale Quest-Framezeiten. Diese Prüfung ist keine Zusage fotorealistischer Grafik oder bereits bestandener V19-Drei-Personen-Abnahme.

### Kurzer Test auf Quest

1. Gewohnt erfassen, X zwei Sekunden, Schrein setzen und starten.
2. Portal nach dem Gegnernachschub im Auge behalten: nach dem Angriffsfenster die zwei leuchtenden Siegel zerschießen. Ab Welle vier sind es drei; Hinweis und Zusatzmunition kontrollieren.
3. Mehrere Bodenrisse vergleichen: Schmiede, zerstörte Brücke, Kathedralenhalle. Decke muss weiterhin ein eigener Schacht sein.
4. Prüfen, ob beide Gegner sauber herauskommen und die Siegel vom normalen Standpunkt gut zu treffen sind. Bei ungeeigneter Sicht pausieren; keine riskanten Ausweichbewegungen.

Danach im Hauptplan: Begegnungsrhythmus und unterscheidbare Gegnerrollen; anschließend Feuerball-Abfangen/Schwachstellen und abgestimmtes Trefferfeedback. Nicht schon als mitgeliefert markieren.
