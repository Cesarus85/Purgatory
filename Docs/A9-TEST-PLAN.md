# A9 – Relikte und Aufnahmeeffekte: Testplan

Dieser Plan prüft A9 auf der tatsächlichen Produktionskette. Der erste eigene
Test muss in einem bekannten synthetischen Corridor mit TSDF-Daten erfolgreich
eine Lebens- und eine Munitionsaufnahme durchführen. Erst danach werden
Ablehnungen geprüft; eine fehlende oder unbekannte Raumkarte darf daher nicht
als scheinbar bestandener Positivtest dienen.

## Editor-Validierung

Die statische Einstiegsklasse ist
`QuestDemonMR.Editor.RelicValidation`:

```text
RelicValidation.Validate()
RelicValidation.QuickValidate()
RelicValidation.ValidateAndExport()
```

`Validate()` ruft zuerst `ShrineValidation.Validate()` auf. Damit bleibt die
vollständige A8-/V18.13-Regressionskette mit ihrem erwarteten **1.155
CHECK**-Baseline-Nachweis aktiv. `QuickValidate()` nutzt die schnelle
Schrein-Prüfung und führt danach dieselbe A9-Suite ohne Android-Export aus.

Für den Export wird ein frisches Verzeichnis verlangt:

```bash
export QDMR_GRADLE_EXPORT="$(mktemp -d /private/tmp/qdmr-v1814-export.XXXXXX)"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -force-metal -quit \
  -projectPath "/Users/stefanmaier/Documents/Codex/2026-09-04/ich-w/outputs/QuestDemonMR" \
  -executeMethod QuestDemonMR.Editor.RelicValidation.ValidateAndExport \
  -logFile "/Users/stefanmaier/Documents/Codex/2026-09-04/ich-w/outputs/QuestDemonMR/Verification/Relics/unity-export.log"
rg -q '^QDMR_RELIC_EXPORT_OK' \
  "/Users/stefanmaier/Documents/Codex/2026-09-04/ich-w/outputs/QuestDemonMR/Verification/Relics/unity-export.log"
```

Der Exportmarker `QDMR_RELIC_EXPORT_OK` zählt nur bei erfolgreicher
Validierung und erfolgreichem Android-Gradle-Export. Vor einer Lieferung sind
zusätzlich Signatur, APK-Metadaten, ABI und der unabhängige Build-/Packaging-
Nachweis zu prüfen; dieser Testplan behauptet keinen Headset-Test.

## Automatisch geprüfte Fälle

A9-Prüfumfang: **59 eigene CHECK-Meldungen**, einschließlich produktivem Triggerpfad während Nachladen, Teilmenge 92→96, vollem Vorrat, unbekannten Samples trotz Ready-Flag, Sofadurchquerung, niedriger Möbelkante und Erhalt der FBX-Achsenausrichtung beim Drehen. Die Gesamtkette ergänzt die 1.155 vorherigen Checks um drei neue Warmup-Ressourcen und A9. Das Audio-Setup prüft zugewiesene Clips und begrenzte Stimmen; es behauptet keine akustische Batchmode-/Headset-Abnahme.

| Bereich | Nachweis |
| --- | --- |
| Asset-Import | Beide FBX-Modelle und das gemeinsame `RelicsV18_14_Albedo` werden mit lesbarer Mesh-Geometrie, ohne importierte Kamera/Lichtobjekte und Android-Texturformat importiert. |
| Ressourcen | Ammo-/Health-Prefab sind getrennte Objekte mit nichtleeren Bounds, Mesh-/Dreiecksbudget und unterschiedlicher Geometriesignatur. Laufzeitmaterialien verwenden den Atlas und `QuestDemonMR/SpatialPBR`. |
| Bedeutung | Bei Gesundheit 90 werden genau 10 Leben gewährt; bei voller Gesundheit 0. Das Relikt bleibt bei 0 erhalten und zeigt `LEBEN VOLL`. Munition gewährt genau 10 bis zum Reserve-Cap 96. |
| Einmaligkeit | Nach erfolgreicher Aufnahme bleiben Collider/Relikt verbraucht; ein erneuter Aufruf liefert 0, auch wenn später wieder Kapazität vorhanden ist. |
| Pause | Direkte Ressourcenänderung und Aufnahme während Pause werden abgewiesen. Die Produktionswaffe kann in Pause keinen Reliktgriff als Schuss ausführen. |
| Fernaufnahme | Ein echter Strahl auf einen sichtbaren Collider nimmt auch bei leerem Magazin auf, verbraucht keine Patrone und erhöht nur die Reserve. Vollaufnahme gibt weiterhin keinen Bonus. |
| Raumgrenzen | `CanReach`/`CanMove` akzeptieren die bekannte freie Corridor-Strecke, blockieren eine Wand, verweigern unbekannte TSDF-Daten und erlauben kein Magnetisieren durch Möbel. |
| Ablegen | `TryDrop` findet den bekannten Boden und eine reale synthetische Sofa-Oberseite; hinter der Vorderwand oder ohne bekannte Unterstützung gibt es keinen Drop. |
| Effekte | `RelicEffects` lädt vier gepoolte Mesh-/Audio-Stimmen, bleibt bei `PoolSize == 4`, erzeugt nur bei positiver Änderung Effekte und löscht sie sofort bei explizitem Clear, Encounter-Reset und App-Pause. |
| Collider/UI | Root-Sphere ist Trigger, Zentrum `(0,.18,0)`, Radius `.16`; Visual und Bedeutungslabel sind getrennte Kinder. Typ, Basisposition und tatsächliche Betragsanzeige werden geprüft. |

## Artefakte

Ein erfolgreicher Editorlauf erzeugt bzw. aktualisiert:

- `Verification/Relics/unity-export.log` (Checks, Fehlergründe, Exportmarker)
- `Verification/Relics/relics-unity.png` (Unity-Vorschau beider Relikte)
- `Verification/Relics/relics-unity-close.png` und `relic-effects-unity.png` (native Nahansicht und beide Effekte mit jeweils 18 nachgewiesenen Meshpartikeln)
- den frischen Exportordner `/private/tmp/qdmr-v1814-export.*`

Die Vorschau bestätigt Import, Atlaszuordnung und grobe Silhouette; sie ersetzt
weder die räumliche Sichtprüfung im Headset noch die Messung von Framerate,
Thermik, Audioverständlichkeit und Haptik.

## Offene reale Abnahme

Die vorhandene Scanner-Ausnahme für den getrackten Körper am Sichtsegment-Endpunkt bleibt unverändert; dort liefert der Sensor absichtlich keine freie Tiefenkarte. Physische Collider blockieren weiterhin. In altem scannerlosem Szenenmodus ist die Prüfung colliderbasiert, nicht sensorbestätigt. Der reguläre Spielstart erstellt den Live-Scanner. Die .16-m-Auswahlkugel ist ein großzügiger Trigger, kein physischer Körper. Drop und Anziehung testen zusätzlich eine konservative Box um die ganze sichtbare Hülle (30 × 33 × 30 cm, samt Drehung und Schweben). Eine BoxCast-Prüfung schützt auch die Strecke vor niedrigen Möbelkanten. Enge reale Kanten gehören trotzdem zur Sichtabnahme und sind nicht allein durch den synthetischen Sofa-Test abgenommen.

Auf einem Quest 3/3S im Zielraum bleibt zu prüfen: Aufnahme aus sicherer
Entfernung und Nähe, Magnetismus an Tisch/Sofa/Wand, keine Durchdringung bei
seitlicher Kopfbewegung, lesbare `+10`/`VOLL`-Anzeige, passender Ton und
Haptik, Pause/Fokus-/Rescan-Cleanup, mehrere Relikte gleichzeitig sowie
langlaufendes Pool-/Frametime-Verhalten. Ein synthetischer TSDF und ein
erfolgreicher Export schließen diese getragenen Gates nicht.
