# V19.11 – Geladenen Raum verwenden / stabilere Erfassung

## Befunde

- Quest-Protokolle V19.10: mehrere konsistente Zwei-Punkt-Ausrichtungen; kein
  erfolgreicher `accepted`-Eintrag. Zwei Ladevorgänge wurden bereits etwa 14 ms nach
  Punkt B abgebrochen. Die Punktbestätigung konnte als Menü-Abbruch weiterwirken.
- Zwei lokale, unveränderte Kopien der tatsächlichen Quest-Profile 2 und 3 wurden
  durch den Produktionsimport geladen. Auf einem 40-cm-Prüfraster gibt es 21 bzw.
  25 durch die alte Freigabe abgelehnte Positionen mit messbarer lokaler Bodenstütze
  und ohne Körperkollision. Diese Rasterzahlen sind keine gemessene Raumfläche.
- Die bisherige Ladefreigabe verlangte nochmals drei zusammenhängende, vollständig
  für Gegner begehbare Sektoren auf einem 1,1-m-Ring. Das ist kein geeigneter Test
  für den tatsächlichen Standort eines Spielers in einem bereits gescannten Raum.
- GPU-Integration und CPU-Auslesen waren an denselben Umlauf gekoppelt. Zusammen
  mit globalem Auslesetakt können dadurch dieselben Abschnitte bevorzugt werden.
- Ein bestätigter TSDF-Oberflächenwert nahe null kann durch einen einzelnen weit
  dahinterliegenden Messwert sein Vorzeichen wechseln und aus dem Mesh verschwinden.
  Dass dies exakt jede beobachtete Lücke erklärt, ist damit noch nicht bewiesen.

## Änderungen

1. Nach manueller A/B-Ausrichtung und visueller Netzbestätigung genügt eine lokal
   nachgewiesene Bodenstütze: mindestens 3 von 5 Strahlen auf einem kleinen
   14-cm-Kreuz, passende gespeicherte Bodenhöhe, keine erfasste Körperkollision.
   Körperhöhe und unbekannter/ungetragener Boden bleiben Sperren. Gegnerwege und
   Portalplätze verwenden weiterhin die bisherigen konservativen Geometrieprüfungen.
2. Diese Trennung gilt auch für die folgenden Readiness-Updates; keine erneute
   Vollscan-Forderung eine halbe Sekunde nach Annahme. Fehlermeldungen nennen den
   konkreten Grund (Boden, Höhe, Hindernis oder fehlende Tiefendaten).
3. Wechsel von Punktaufnahme zum Menü erfordert losgelassene Tasten. Absichtliches
   Abbrechen mit einem neuen Tastendruck bleibt möglich.
4. GPU-Auslesen unabhängig vom Integrationsumlauf, älteste noch nicht gelesene
   Beobachtung zuerst, keine doppelten Transfers oder Überschreibung laufender Jobs.
   Auch die sechs Teilmengen der Tiefenbild-Entdeckung wechseln jetzt explizit;
   sie hängen nicht mehr vom Framezähler modulo sechs ab, der bei festen
   Headset-Bildraten immer wieder dieselben Bildspalten auswählen konnte.
5. Höheres Scan-Budget nur während der Ersterfassung: Auslesen frühestens nach
   12,5 statt 25 ms; Mesh-Commit frühestens nach 20 statt 40 ms. Weiter maximal
   ein Auslesen und ein Commit pro Frame. Platzierung und Gameplay behalten ihre
   vorherigen Budgets. Dies ist keine FPS-Garantie; Quest-Profiling bleibt nötig.
6. Transiente Hintergrundwerte dürfen eine bestätigte Oberfläche nicht sofort
   entfernen. Drei gültige widersprechende Beobachtungen erlauben weiterhin das
   Entfernen tatsächlich verschobener Gegenstände. Zusätzliche Evidenz ist flüchtig,
   4 Byte je GPU-Voxel, ungefähr 2,4 MiB beim maximalen GPU-Pool.

## Test und Übergabe

Bestehende mit V19.10 gespeicherte A/B-Räume bleiben unverändert nutzbar. Für den
Ladetest ist kein neuer Scan erforderlich. Separat kann ein neuer Scan zum Vergleich
der Erfassungsgeschwindigkeit und Oberflächenstabilität erstellt werden.

- Raum 2 oder 3 laden, A/B setzen, Button nur frisch drücken, Netz kontrollieren.
- „Raum verwenden“ auf einem sichtbaren Bodenabschnitt: Übergang zum Schrein prüfen.
- Abweichende Standpositionen sowie absichtlichen Abbruch testen.
- Neuen Scan drehen/schwenken, bereits erfasste Wände erneut ansehen. Einzelne
  verrauschte Bilder sollen keine fertige Oberfläche sofort entfernen; verschobene
  Gegenstände müssen bei anhaltender Beobachtung trotzdem aktualisiert werden.

APK-Ziel: `Builds/QuestDemonMR-v19.11-scan-acceptance.apk`, Version 0.19.11 / Code 50.
Build: `bash Tools/build-v19.11.sh`. Logs und lokale Geräteprofil-Kopien:
`Verification/ScanAcceptance`. Die Originale auf der Quest werden nicht verändert.

### Automatisierte Prüfung am 07.09.2026

- Aktueller V19.11-Quellstand: 2.721 erfolgreiche `QDMR_*CHECK`-Prüfschritte
  einschließlich 90 neuer Freigabe-/Scanprüfungen und echter Compute-GPU-Prüfung
  unter Metal. Android/Vulkan wird zusätzlich im Paketbau kompiliert.
- Log: `Verification/ScanAcceptance/unity-export-qdmr-v1911-export.HcgBOm.log`.
- Tatsächliche Raumprofile 2 und 3 wurden unverändert importiert. Alte und neue
  Freigabe wurden an denselben Rasterpositionen verglichen; anschließend wurden
  zuvor abgelehnte, lokal unterstützte Positionen erfolgreich angenommen.
- SHA-256 der unveränderten Profilkopien und Geräteoriginale:
  - Raum 2: `440e757a005e5b01febe6ccfd90bbd162210522ee529440e73b98230c717417e`
  - Raum 3: `8ab1fdc5666e60ea58f5ca399779d4ce8cf83f9836ef3791ad46f57169250140`
- Kein getragener Quest-Test dieses Stands: Akzeptanz im echten Raum, tatsächliche
  Scan-Geschwindigkeit, Framerate und Stabilität unter Sensorrauschen bleiben offen.

### Fertiges Paket

- Unity-Export erfolgreich; keine C#-Compilerfehler, Shaderfehler oder Exceptions.
  Gradle `assembleRelease` erfolgreich (3m 22s, 117 Tasks).
- Paket `de.stefanmaier.questdemonmr`, Version `0.19.11`, Code `50`, 106.636.398 Byte.
- APK-SHA-256: `9e93a1773db38519c261f68dbab411528a9bfe97693eb8d20d8947c097dc10c5`.
- APK-ZIP fehlerfrei, Signatur gültig und identisch zum V19.10-Entwicklerzertifikat:
  `3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2`.
- Eingebettete ARM64-`libil2cpp.so` stimmt mit dem neu kompilierten Paketstand überein:
  `19d5a283c6be2b025f50759f33c01968aba50d5984c61e53caa895bf6c76da30`.
- Export: `/private/tmp/qdmr-v1911-export.HcgBOm`.
- Nicht auf der Quest installiert. Update-Installation statt Deinstallation verwenden,
  damit bestehende Räume und Einstellungen erhalten bleiben.
