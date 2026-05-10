## 1. Kontext projektu a doména

- **Předmět a cíl:** Školní projekt (MSWA), jehož cílem je vytvořit aplikaci pro monitorování a orchestraci datových pipeline (tzv. Control Plane). Aplikace simuluje reálné systémy, ale neprovádí skutečné distribuované výpočty.
- **Doména:** Poskytovatel flotily autonomních taxi.
- **Data (simulovaná):** Telemetrie vozů, nabíjecí cykly, záznamy o jízdách a prediktivní údržba.

## 2. Technologický stack

- **Backend:** .NET 10 Minimal API.
- **Frontend:** Next.js (App Router), Tailwind CSS, shadcn/ui.
- **Databáze:** MongoDB s oficiálním .NET driverem.
- **Real-time komunikace:** SignalR.
- **Infrastruktura:** Monorepo (backend i frontend v jednom repozitáři).

## 3. Architektura a Designové principy (Best Practices)

- **Push Model (Event-Driven):** Zcela bez pollingu. Pipeline při změně stavu aktivně vyzařuje události ("šéf úkoluje asistentku") přes in-memory sběrnici (MediatR).
- **SignalR Batching (Dávkování):** Události se sbírají do bufferu (System.Threading.Channels) a na frontend se odesílají periodicky v dávkách, aby nedošlo k zahlcení sítě.
- **Separation of Concerns:** Oddělení řízení (Orchestrátor) od výkonu. Běh výpočetního enginu je pro účely projektu simulován asynchronními tasky na pozadí.
- **Autentizace:** Neimplementuje se reálně, UI natvrdo simuluje přihlášeného "Admina".

## 4. Datový model a doménová pravidla

- **Datasets:** Metadata datových zdrojů (např. `vehicle-telemetry`).
- **Pipelines & Verzování:** Odkazují na dataset. Každá pipeline má vnořené pole verzí (pouze jedna smí být `active: true`).
- **AlertRules (Pravidla):** Samostatná kolekce navázaná na ID pipeline (nejsou vnořená do verzí, čímž oddělujeme logiku alertů od datových transformací).
- **JobRuns a Alerts:** `JobRun` je instance běhu. Pokud selže nebo poruší pravidlo (např. timeout), systém to zachytí a vygeneruje záznam do kolekce `Alerts`.

## 5. Funkcionalita aplikace

- **Dashboard:** Přehledy (počty pipeline, běžících úloh, alertů).
- **CRUD operace:** Správa datasetů, pipeline a pravidel alertů.
- **Manuální spouštění:** Operátor spouští úlohy (vytvoří `JobRun` a odstartuje backendovou simulaci).
- **Monitoring:** Real-time zobrazení historie, progresu, statusu a detailů alertů přes SignalR.
