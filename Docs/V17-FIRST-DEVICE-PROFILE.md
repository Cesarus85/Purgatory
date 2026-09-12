# V17 – erstes tatsächliches Quest-Profil

Aktueller Nachtrag: Der erste V17.2-Lauf ist vollständig ausgewertet, einschließlich des eingeschränkten Vergleichs wegen zwei statt drei Gegnern und fehlender Startup-/GPU-Protokolle. Siehe [V17.2-Gerätelauf](REVIEW-V17-2.md#erster-tatsächlicher-v172-gerätelauf). Die folgenden drei Läufe bleiben historische Vergleichsdaten.

5. September 2026, Quest 3, Vulkan, Development-APK `0.17.0` / Code 17.

APK SHA-256: `80ad23863f9ba1d84575ef37c8399140a9e44c3a09e1583f0f95d40e1120a48e`.

Der Nutzer startete den Test am getragenen Headset. Raum wurde geladen (41 Anker, 9 Wände), Live Depth meldete Bereitschaft. Der komplette 60-Sekunden-Lauf endete normal. Rohdaten: `Verification/V17/Quest-Development-First/`, ursprüngliche Sitzung `v17-20260905-120442-812213e0`. CSV-Zeiten sind relativ; Sitzungsname ist UTC. Keine synthetischen Daten.

| Phase | Frame-Mittel | größtes Fenster-P95 | Maximum | Mesh-Bakes |
| --- | ---: | ---: | ---: | ---: |
| Baseline | 13,890 ms | 15,25 ms | 16,023 ms | 0 |
| Ein Portal | 14,265 ms | 26,00 ms | 222,242 ms | 0 |
| Zwei Portale angefordert, nur eines vorhanden | 13,885 ms | 15,00 ms | 16,781 ms | 0 |
| Gegner | 14,018 ms | 15,50 ms | 152,760 ms | 0 |
| Treffer/Effekte | 15,907 ms | 38,25 ms | 55,003 ms | 1433 |

## Grenzen der Aussage

- Nur ein sicher platzierbares Portal, zwei statt drei Gegner (Emberfiend und Rift Bat). Zweites Portal und ein Gegner wurden durch die unveränderten Raum-Sicherheitsprüfungen übersprungen. Kein Zweipor­tal-Stresstest bestanden.
- CPU aus `FrameTimingManager` liegt fast gleichauf mit der Unity-Framezeit; diese Zahl ist keine isolierte aktive CPU-Arbeitszeit und kann Warte-/Synchronisationszeit enthalten. GPU-Zeiten wurden über den XR-Aufruf nicht geliefert und bleiben unbekannt.
- Rund die Hälfte der Baseline-Frames liegt minimal oberhalb der nominalen 72-Hz-Schwelle. Das ist **nicht** gleichbedeutend mit 50 % verlorenen Bildern: das enge Schwellwert-Kriterium zählt auch normales Timing-Jitter. Es ersetzt keine Compositor-/Stale-Frame-Messung.
- Große Spitzen fallen in Inhalts-Erzeugungsphasen; Ressourcenaufbau/erster Gebrauch ist ein Kandidat, noch kein isoliert bewiesener Engpass.
- Sichtbarkeit, Kopfbewegung, Platzierungen und Development-Overhead begrenzen Vergleiche. Kein Langzeit-/Thermikprofil und kein allgemeiner 72-Hz-Freigabenachweis.

## Konkreter Optimierungskandidat

Im Kampf wurden 1.433 vollständige Mesh-Bakes und 1.010.790 Dreieckstests in 15 Sekunden registriert. Quellcode erklärt die häufigen Bakes: `SurfaceWound.LateUpdate` fordert für kurzlebige Wundflecken jede animierte Vollhaut erneut an; `CombatSurface` teilt dies bereits zwischen Wunden derselben Haut im selben Frame, aber nicht mit der GPU-Darstellung. [Unity dokumentiert BakeMesh als CPU-Skinning](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html).

Gezielte Optimierung: exakte, gewichtete Knochenverformung nur der Wundpunkte. Der vollständige Mesh-Schusstest bleibt unverändert. Blendshapes, Cloth und mehr als vier gespeicherte Gewichte pro Punkt behalten den bisherigen exakten BakeMesh-Pfad. Ein-/Zwei-Knochen-Qualität berücksichtigt die abgeschnittenen und neu normierten Einflüsse, statt unbemerkt die Editor-Vier-Knochen-Pose zu verwenden.

57 zusätzliche native Unity-Prüfungen bestanden: alle 15 Produktionsclips beider Modelle in vier Posen und drei Knochen-Qualitätsstufen; nichtuniforme Skalierung; Übereinstimmung mit `BakeMesh` innerhalb 1 mm; keine verwaltete Allokation in 200 warmgelaufenen Sparse-Updates; sichere Fallbacks. Logs: `work/unity-v17-sparse-validation2.log` und vollständiger neuer Regressionslauf `work/unity-v17-optimized-build.log`. Insgesamt jetzt 160 native Regressionstests. Der erste Prüfentwurf lehnte die Android-Zwei-Knochen-Qualität noch ab; diese Lücke wurde vor Integration korrigiert.

Die Optimierung ist in den Quellstand `0.17.1` integriert; ihr APK-/Installationsstatus steht im Buildbericht. Keine Grafikauflösung, Renderqualität, Treffer-Toleranz oder Wund-Lebensdauer wurde dafür abgesenkt. Der Gen-0-GC-Zähler ersetzt außerdem die fehleranfällige Summe aller Generationen. Die älteren `0.17.0`-CSV-Werte für `gc_collections` sind diese Summe und dürfen nicht direkt als Anzahl einzelner Bereinigungen interpretiert oder mit `0.17.1` verglichen werden.

Der ursprüngliche Lauf zeigte eine Lastkorrelation, keine vollständige Profiler-Aufschlüsselung. Der folgende dritte Lauf liefert nun einen tatsächlichen Vorher-/Nachher-Nachweis der Bake-Zahl, jedoch keinen behaupteten FPS-Gewinn oberhalb des bereits erreichten 72-Hz-Ziels.

Reproduktion: `python3 Tools/analyze_v17.py Verification/V17/Quest-Development-First`.

## Zweiter Lauf: Nicht-Development-Basis

Unverändertes Gameplay `0.17.0`, kein Development-Modus. APK SHA-256 `a3dcff7467fd899d4bd5e36407c51d30330417c1fc91472f130d74a73ff108db`, 83734686 Bytes. Rohdaten: `Verification/V17/Quest-Measurement-Baseline/`; Sitzung `v17-20260905-122422-9f54151e`, normal abgeschlossen.

Diesmal waren zwei Portale sichtbar und drei Gegner vorhanden; keine Inhalte wurden übersprungen. Das ist ein vollständigerer Lastlauf als der erste, aber wegen anderer Platzierungen und Kopfbewegung kein identischer A/B-Replay.

| Phase | Frame-Mittel | größtes Fenster-P95 | Maximum |
| --- | ---: | ---: | ---: |
| Baseline | 13,889 ms | 15,25 ms | 16,611 ms |
| Ein Portal | 13,941 ms | 16,25 ms | 55,761 ms |
| Zwei Portale | 13,881 ms | 15,50 ms | 16,559 ms |
| Drei Gegner | 14,005 ms | 16,00 ms | 139,980 ms |
| Treffer/Effekte | 13,889 ms | 15,75 ms | 18,351 ms |

Die Kampfphase registrierte 1705 Vollhaut-Bakes und 1115760 Dreieckstests. Im Mittel wird bereits das 72-Hz-Ziel erreicht; die Sparse-Optimierung zielt hier auf Reserve, nicht auf eine behauptete höhere Bildrate. Erzeugungsspitzen bleiben ein separates Thema.

### Ergänzende Runtime-GPU-Metriken, nur ein kurzer Ausschnitt

Der XR-CSV-Aufruf liefert weiter keine GPU-Zeit. Zusätzlich wurden echte `VrApi`-Zeilen **des Spielprozesses 24650** gesichert (`vrapi.log`). Der Android-Ringpuffer enthielt bei Abruf nur noch das Ende der Kampfphase und nachfolgende Daten. Nur die sieben Zeilen von 14:25:15 bis 14:25:21 gehören sicher vollständig in die Kampfphase: mittlere `App`-GPU-Zeit 7,397 ms; einzelne `Stale`-Werte 1, 0, 0, 0, 2, 0, 1; erste gemeldete Temperaturkomponente 38 °C. Keine Aussage über den gesamten Lauf und keine Langzeit-Thermikfreigabe. Nachfolgende Pausen-/Spielzeilen dürfen nicht in den Benchmark gemittelt werden.

Metas [Definition der VrApi-Metriken](https://developers.meta.com/horizon/blog/ovr-metrics-tool-vrapi-what-do-these-metrics-mean/) ordnet `App` der GPU-Zeit in Millisekunden zu. Die gemeldeten Stale-Werte zeigen auch, warum das Framezeit-Mittel allein keine vollständige Komfort-Abnahme ist.

## Dritter Lauf: V17.1 mit Sparse-Wunden

Optimierter Nicht-Development-Build `0.17.1`, SHA-256 `cffcc92ebf25cd310c0815d9a02cf18d9d5642ff3c4b46afc0fdbf6b267eae52`. Rohdaten: `Verification/V17/Quest-Measurement-Optimized/`, Sitzung `v17-20260905-123555-93fc2a8c`. Normal nach 60,003 Sekunden beendet; zwei sichtbare Portale, drei Gegner, keine übersprungenen Inhalte. Alle drei Gegneroberflächen protokollieren `wound_skinning,sparse_exact`.

| Kampfphase, jeweils 15 Sekunden | V17.0 Messbasis | V17.1 optimiert |
| --- | ---: | ---: |
| Frames | 1080 | 1080 |
| Framezeit-Mittel | 13,889 ms | 13,887 ms |
| größtes Fenster-P95 | 15,75 ms | 15,25 ms |
| Maximum | 18,351 ms | 18,281 ms |
| Vollhaut-Bakes | 1705 | **90** |
| Exakt geprüfte Dreiecke | 1115760 | **1115760** |
| Unity-Allocator-Spitze | 129334720 Bytes | 129790119 Bytes |

**Verifiziertes Ergebnis:** 94,72 % weniger vollständige Haut-Bakes bei unveränderter Zahl exakter Dreieckstests. Zusätzliche Cache-Daten erhöhen den beobachteten Unity-Allocator-Peak leicht (rund 0,43 MiB). Beide Läufe erreichen bereits rund 72 Hz; kleine Differenzen von Mittel/P95/Maximum sind bei je einem Lauf kein Nachweis einer allgemeinen FPS-Verbesserung. Kein Qualitätsabbau als Abkürzung; die mathematische Oberflächentreue wurde separat nativ geprüft. Eine visuelle Nutzerabnahme von Wunden in allen Posen bleibt davon getrennt.

Die Erzeugungsspitzen bleiben: erstes Portal maximal 43,437 ms, Gegnererzeugung maximal 138,898 ms. Nächster gezielter Kandidat ist Vorladen/verteilter Aufbau beziehungsweise Pooling dieser Ressourcen mit sauberem Speicherbudget, nicht eine pauschale Reduzierung der Portalauflösung.

### Zusätzliche VrApi-Stichproben

66 eindeutige Zeilen des aktuellen Spielprozesses 15326 wurden aus mehreren Abrufen zusammengeführt. Es gibt eine Ringpuffer-Lücke; daher kein vollständiges externes GPU-/Compositor-Profil behauptet. Für den sicheren inneren Kampfzeitraum 14:36:42–14:36:54 liegen 13 Ein-Sekunden-Zeilen vor: mittlere App-GPU-Zeit 7,927 ms, Maximum 8,05 ms, `Stale=0` in allen 13 Zeilen. Die Zwei-Portal-Phase ohne Gegner fehlt in diesem externen Ausschnitt, ist aber vollständig in der Unity-CSV enthalten. Kopfbewegung und unterschiedliche Blickwinkel verhindern einen exakten GPU-A/B-Vergleich mit dem kurzen Vorher-Ausschnitt.

**Noch offene V17-Gates:** gezielte Abbruch-/Fokus-/Pause-Prüfungen, weitere Raumgröße, mindestens ein 20-Minuten-Lauf und vollständigeres externes CPU-/GPU-/Compositor-/Thermikprofil. Drei kurze Messläufe im gleichen gescannten Raum ersetzen diese Gates nicht.
