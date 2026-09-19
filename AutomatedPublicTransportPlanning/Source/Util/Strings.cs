using System.Collections.Generic;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Every user-facing string in the mod, in each language it has been translated
    /// into. English is the source language; the rest are additions to it.
    ///
    /// Adding a language is one method plus one line in BuildTables. Correcting a
    /// translation is one line in one method — the tables are grouped by language
    /// rather than by key so that a contributor who speaks one of them can find their
    /// block, change it, and touch nothing else.
    ///
    /// TRANSLATION STATUS: English is authoritative. The other ten were not written by
    /// native speakers and are offered as a starting point; corrections are welcome and
    /// wanted. See the project README for how to send one.
    ///
    /// The mod's NAME is deliberately absent. It is how the mod is found on the
    /// workshop and how players refer to it to each other, so it stays in English in
    /// every language, the way software product names usually do.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Language tags, in the order a picker should offer them. The stored setting
        /// and the dropdown both read this, so they cannot drift apart.
        /// </summary>
        internal static readonly string[] LanguageOrder =
        {
            "en", "zh_TW", "zh_CN", "ja", "ko", "de", "fr", "es", "pt-BR", "ru", "pl"
        };

        /// <summary>
        /// What each language calls itself, in the same order.
        ///
        /// These are NOT translated and must never be. Someone looking for their own
        /// language needs to find it written the way they write it — rendering the list
        /// in whatever language the panel happens to be in right now would hide the very
        /// entry they are hunting for.
        /// </summary>
        internal static readonly string[] LanguageNames =
        {
            "English", "繁體中文", "简体中文", "日本語", "한국어",
            "Deutsch", "Français", "Español", "Português", "Русский", "Polski"
        };

        public const string LabelLanguage = "label.language";
        public const string ModDescription = "mod.description";
        public const string GroupDiagnostics = "group.diagnostics";
        public const string ButtonTestLog = "btn.testLog";
        public const string ButtonBusPrefabs = "btn.busPrefabs";
        public const string ButtonMeasure = "btn.measure";
        public const string GroupSpike = "group.spike";
        public const string CheckSpikeArm = "check.spikeArm";
        public const string ButtonSpikeCreate = "btn.spikeCreate";
        public const string ButtonSpikeReport = "btn.spikeReport";
        public const string ButtonSpikeRemove = "btn.spikeRemove";

        internal static Dictionary<string, Dictionary<string, string>> BuildTables()
        {
            Dictionary<string, Dictionary<string, string>> tables =
                new Dictionary<string, Dictionary<string, string>>();

            tables["en"] = English();
            tables["zh_TW"] = TraditionalChinese();
            tables["zh_CN"] = SimplifiedChinese();
            tables["ja"] = Japanese();
            tables["ko"] = Korean();
            tables["de"] = German();
            tables["fr"] = French();
            tables["es"] = Spanish();
            tables["pt-BR"] = Portuguese();
            tables["ru"] = Russian();
            tables["pl"] = Polish();

            return tables;
        }

        // ------------------------------------------------------------- English (source)

        private static Dictionary<string, string> English()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Language";
            d[ModDescription] = "Plans bus lines from city demand. Previews first; nothing is built until you accept a line.";
            d[GroupDiagnostics] = "Diagnostics";
            d[ButtonTestLog] = "Write a test line to the log";
            d[ButtonBusPrefabs] = "List bus prefabs in detail";
            d[ButtonMeasure] = "Measure planning costs (brief pause)";
            d[GroupSpike] = "Spike 1 - save round trip (writes to your city)";
            d[CheckSpikeArm] = "Enable these buttons - they change the city you have loaded";
            d[ButtonSpikeCreate] = "1. Create a test bus line";
            d[ButtonSpikeReport] = "2. Report test lines";
            d[ButtonSpikeRemove] = "3. Remove test lines";
            return d;
        }

        // ------------------------------------------------------------- zh_TW 繁體中文

        private static Dictionary<string, string> TraditionalChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "語言";
            d[ModDescription] = "依城市需求規劃公車路線。先預覽，你確認之後才會實際建立。";
            d[GroupDiagnostics] = "診斷";
            d[ButtonTestLog] = "將測試路線寫入記錄檔";
            d[ButtonBusPrefabs] = "列出公車 prefab 詳細資料";
            d[ButtonMeasure] = "量測規劃成本（會短暫停頓）";
            d[GroupSpike] = "Spike 1 - 存檔往返測試（會寫入你的城市）";
            d[CheckSpikeArm] = "啟用這些按鈕 - 它們會變更你已載入的城市";
            d[ButtonSpikeCreate] = "1. 建立測試公車路線";
            d[ButtonSpikeReport] = "2. 回報測試路線";
            d[ButtonSpikeRemove] = "3. 移除測試路線";
            return d;
        }

        // ------------------------------------------------------------- zh_CN 简体中文

        private static Dictionary<string, string> SimplifiedChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "语言";
            d[ModDescription] = "根据城市需求规划公交线路。先预览，你确认之后才会实际建立。";
            d[GroupDiagnostics] = "诊断";
            d[ButtonTestLog] = "将测试线路写入日志";
            d[ButtonBusPrefabs] = "列出公交 prefab 详细信息";
            d[ButtonMeasure] = "测量规划开销（会短暂停顿）";
            d[GroupSpike] = "Spike 1 - 存档往返测试（会写入你的城市）";
            d[CheckSpikeArm] = "启用这些按钮 - 它们会更改你已载入的城市";
            d[ButtonSpikeCreate] = "1. 创建测试公交线路";
            d[ButtonSpikeReport] = "2. 报告测试线路";
            d[ButtonSpikeRemove] = "3. 移除测试线路";
            return d;
        }

        // ------------------------------------------------------------- 日本語

        private static Dictionary<string, string> Japanese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "言語";
            d[ModDescription] = "都市の需要からバス路線を計画します。まずプレビューを表示し、承認するまで何も建設しません。";
            d[GroupDiagnostics] = "診断";
            d[ButtonTestLog] = "テスト路線をログに書き出す";
            d[ButtonBusPrefabs] = "バスのプレハブを詳細表示";
            d[ButtonMeasure] = "計画処理のコストを計測（一時的に停止します）";
            d[GroupSpike] = "Spike 1 - セーブ往復テスト（都市に書き込みます）";
            d[CheckSpikeArm] = "これらのボタンを有効にする - 読み込み中の都市が変更されます";
            d[ButtonSpikeCreate] = "1. テストバス路線を作成";
            d[ButtonSpikeReport] = "2. テスト路線を報告";
            d[ButtonSpikeRemove] = "3. テスト路線を削除";
            return d;
        }

        // ------------------------------------------------------------- 한국어

        private static Dictionary<string, string> Korean()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "언어";
            d[ModDescription] = "도시 수요를 바탕으로 버스 노선을 계획합니다. 먼저 미리 보기를 제공하며, 수락하기 전에는 아무것도 건설하지 않습니다.";
            d[GroupDiagnostics] = "진단";
            d[ButtonTestLog] = "테스트 노선을 로그에 기록";
            d[ButtonBusPrefabs] = "버스 프리팹 상세 목록";
            d[ButtonMeasure] = "계획 비용 측정 (잠시 멈춤)";
            d[GroupSpike] = "Spike 1 - 저장 왕복 테스트 (도시에 기록됩니다)";
            d[CheckSpikeArm] = "이 버튼들을 활성화 - 불러온 도시가 변경됩니다";
            d[ButtonSpikeCreate] = "1. 테스트 버스 노선 생성";
            d[ButtonSpikeReport] = "2. 테스트 노선 보고";
            d[ButtonSpikeRemove] = "3. 테스트 노선 제거";
            return d;
        }

        // ------------------------------------------------------------- Deutsch

        private static Dictionary<string, string> German()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Sprache";
            d[ModDescription] = "Plant Buslinien anhand des Bedarfs der Stadt. Zeigt zuerst eine Vorschau; es wird nichts gebaut, bevor Sie eine Linie annehmen.";
            d[GroupDiagnostics] = "Diagnose";
            d[ButtonTestLog] = "Testlinie ins Protokoll schreiben";
            d[ButtonBusPrefabs] = "Bus-Prefabs im Detail auflisten";
            d[ButtonMeasure] = "Planungskosten messen (kurze Pause)";
            d[GroupSpike] = "Spike 1 - Speicher-Rundlauf (schreibt in Ihre Stadt)";
            d[CheckSpikeArm] = "Diese Schaltflächen aktivieren - sie verändern die geladene Stadt";
            d[ButtonSpikeCreate] = "1. Testbuslinie erstellen";
            d[ButtonSpikeReport] = "2. Testlinien melden";
            d[ButtonSpikeRemove] = "3. Testlinien entfernen";
            return d;
        }

        // ------------------------------------------------------------- Français

        private static Dictionary<string, string> French()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Langue";
            d[ModDescription] = "Planifie des lignes de bus à partir de la demande de la ville. Aperçu d'abord ; rien n'est construit tant que vous n'avez pas accepté une ligne.";
            d[GroupDiagnostics] = "Diagnostic";
            d[ButtonTestLog] = "Écrire une ligne de test dans le journal";
            d[ButtonBusPrefabs] = "Lister les prefabs de bus en détail";
            d[ButtonMeasure] = "Mesurer les coûts de planification (courte pause)";
            d[GroupSpike] = "Spike 1 - aller-retour de sauvegarde (écrit dans votre ville)";
            d[CheckSpikeArm] = "Activer ces boutons - ils modifient la ville chargée";
            d[ButtonSpikeCreate] = "1. Créer une ligne de bus de test";
            d[ButtonSpikeReport] = "2. Signaler les lignes de test";
            d[ButtonSpikeRemove] = "3. Supprimer les lignes de test";
            return d;
        }

        // ------------------------------------------------------------- Español

        private static Dictionary<string, string> Spanish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Planifica líneas de autobús según la demanda de la ciudad. Primero muestra una vista previa; no se construye nada hasta que aceptes una línea.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escribir una línea de prueba en el registro";
            d[ButtonBusPrefabs] = "Listar los prefabs de autobús en detalle";
            d[ButtonMeasure] = "Medir los costes de planificación (breve pausa)";
            d[GroupSpike] = "Spike 1 - ida y vuelta de guardado (escribe en tu ciudad)";
            d[CheckSpikeArm] = "Activar estos botones - modifican la ciudad que tienes cargada";
            d[ButtonSpikeCreate] = "1. Crear una línea de autobús de prueba";
            d[ButtonSpikeReport] = "2. Informar de las líneas de prueba";
            d[ButtonSpikeRemove] = "3. Eliminar las líneas de prueba";
            return d;
        }

        // ------------------------------------------------------------- Português

        private static Dictionary<string, string> Portuguese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Planeja linhas de ônibus a partir da demanda da cidade. Mostra uma prévia primeiro; nada é construído até você aceitar uma linha.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escrever uma linha de teste no log";
            d[ButtonBusPrefabs] = "Listar os prefabs de ônibus em detalhe";
            d[ButtonMeasure] = "Medir os custos de planejamento (breve pausa)";
            d[GroupSpike] = "Spike 1 - ida e volta de salvamento (escreve na sua cidade)";
            d[CheckSpikeArm] = "Ativar estes botões - eles alteram a cidade carregada";
            d[ButtonSpikeCreate] = "1. Criar uma linha de ônibus de teste";
            d[ButtonSpikeReport] = "2. Relatar as linhas de teste";
            d[ButtonSpikeRemove] = "3. Remover as linhas de teste";
            return d;
        }

        // ------------------------------------------------------------- Русский

        private static Dictionary<string, string> Russian()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Язык";
            d[ModDescription] = "Планирует автобусные маршруты по спросу города. Сначала показывает предпросмотр; ничего не строится, пока вы не примете маршрут.";
            d[GroupDiagnostics] = "Диагностика";
            d[ButtonTestLog] = "Записать тестовый маршрут в журнал";
            d[ButtonBusPrefabs] = "Подробный список префабов автобусов";
            d[ButtonMeasure] = "Измерить затраты на планирование (короткая пауза)";
            d[GroupSpike] = "Spike 1 - цикл сохранения и загрузки (записывает в ваш город)";
            d[CheckSpikeArm] = "Включить эти кнопки - они изменяют загруженный город";
            d[ButtonSpikeCreate] = "1. Создать тестовый автобусный маршрут";
            d[ButtonSpikeReport] = "2. Отчёт по тестовым маршрутам";
            d[ButtonSpikeRemove] = "3. Удалить тестовые маршруты";
            return d;
        }

        // ------------------------------------------------------------- Polski

        private static Dictionary<string, string> Polish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Język";
            d[ModDescription] = "Planuje linie autobusowe na podstawie zapotrzebowania miasta. Najpierw pokazuje podgląd; nic nie powstaje, dopóki nie zaakceptujesz linii.";
            d[GroupDiagnostics] = "Diagnostyka";
            d[ButtonTestLog] = "Zapisz linię testową do dziennika";
            d[ButtonBusPrefabs] = "Wypisz szczegóły prefabrykatów autobusów";
            d[ButtonMeasure] = "Zmierz koszty planowania (krótka pauza)";
            d[GroupSpike] = "Spike 1 - test zapisu i wczytania (zapisuje w Twoim mieście)";
            d[CheckSpikeArm] = "Włącz te przyciski - zmieniają wczytane miasto";
            d[ButtonSpikeCreate] = "1. Utwórz testową linię autobusową";
            d[ButtonSpikeReport] = "2. Zgłoś linie testowe";
            d[ButtonSpikeRemove] = "3. Usuń linie testowe";
            return d;
        }
    }
}
