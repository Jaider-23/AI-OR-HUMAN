using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera el banco de casos inicial (GDD 16 y 17) como assets editables en Assets/Cases.
/// Si un caso ya existe se sobrescriben sus datos para mantenerlo sincronizado.
/// </summary>
public static class CaseBankBuilder
{
    public const string CasesFolder = "Assets/Cases";

    static ToolFinding F(InvestigationTool tool, string result, bool relevant) =>
        new ToolFinding { tool = tool, result = result, isRelevant = relevant };

    public static CaseData[] BuildAll()
    {
        BuildPaths.EnsureFolder(CasesFolder);
        var result = new List<CaseData>();
        foreach (var spec in Specs()) result.Add(Save(spec));
        AssetDatabase.SaveAssets();
        return result.ToArray();
    }

    /// <summary>
    /// Carga los casos que ya están en disco, sin regenerarlos (ordenados por nivel como
    /// los espera el GameManager). Lo usan las herramientas que solo tocan la interfaz.
    /// </summary>
    public static CaseData[] LoadExisting()
    {
        if (!AssetDatabase.IsValidFolder(CasesFolder)) return new CaseData[0];
        return AssetDatabase.FindAssets("t:CaseData", new[] { CasesFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<CaseData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(data => data != null)
            .OrderBy(data => data.level)
            .ThenBy(data => data.caseId)
            .ToArray();
    }

    static CaseData Save(CaseData spec)
    {
        string path = $"{CasesFolder}/{spec.caseId}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CaseData>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(spec, path);
            return spec;
        }
        EditorUtility.CopySerialized(spec, existing);
        existing.name = spec.caseId;
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(spec);
        return existing;
    }

    static CaseData Case(string id, int level, ContentType type, string title, string source, string body,
                         Verdict correct, Verdict ada, string adaHint, Speaker speaker, string intro,
                         string feedback, bool canShare, params ToolFinding[] findings)
    {
        var data = ScriptableObject.CreateInstance<CaseData>();
        data.name = id;
        data.caseId = id;
        data.level = level;
        data.contentType = type;
        data.title = title;
        data.sourceLabel = source;
        data.contentBody = body;
        data.correctVerdict = correct;
        data.adaSuggestion = ada;
        data.adaHint = adaHint;
        data.introSpeaker = speaker;
        data.introDialogue = intro;
        data.feedbackExplanation = feedback;
        data.canShare = canShare;
        data.findings = findings;
        return data;
    }

    static IEnumerable<CaseData> Specs()
    {
        yield return Case("C-101", 1, ContentType.Text, "Mensaje de un compañero",
            "Chat · Andrés (contacto guardado)",
            "oye parce mañana no llego al entreno, se me volvio a dañar la moto jaja :)\nle dices al profe Ramírez q le mando el informe el viernes? graciasss",
            Verdict.Human, Verdict.Human,
            "Lenguaje coloquial, errores naturales y detalles personales. Me inclino por HUMANO.",
            Speaker.Ada, "Empecemos con algo sencillo. Un mensaje de chat llegó a la bandeja de pruebas.",
            "Era HUMANO. Señales: errores tipográficos espontáneos, jerga local (\"parce\"), referencias concretas a personas y hechos verificables. Ojo: la informalidad sola no prueba nada, pero sumada a un contacto conocido y un contexto coherente apunta a un humano.",
            false,
            F(InvestigationTool.AnalyzeText, "Errores espontáneos (\"volvio\", \"q\"), jerga regional y un emoticono usado con ironía. Patrón típico de escritura humana rápida.", true),
            F(InvestigationTool.ViewSource, "Contacto guardado desde 2021 con historial de conversaciones coherente.", true),
            F(InvestigationTool.AnalyzeProfile, "Perfil personal con fotos de varios años.", false));

        yield return Case("C-102", 1, ContentType.Text, "Reseña de restaurante",
            "Plataforma de reseñas · Usuario \"GourmetReview_88\"",
            "Una experiencia culinaria verdaderamente excepcional que deleita todos los sentidos. El ambiente es acogedor y sofisticado, el servicio es impecable y cada plato es una obra maestra. Sin duda, un lugar que recomiendo ampliamente a todos los amantes de la buena gastronomía.",
            Verdict.AI, Verdict.AI,
            "Frases genéricas y perfectas, sin un solo detalle concreto. Probablemente IA.",
            Speaker.Ada, "Una reseña muy entusiasta. ¿Quién la escribió realmente?",
            "Era IA. El texto no menciona ningún plato, precio ni anécdota: solo adjetivos genéricos con estructura impecable. Además, el perfil publicó decenas de reseñas con el mismo estilo en dos días. Ninguna señal aislada es prueba definitiva, pero juntas forman un patrón claro.",
            false,
            F(InvestigationTool.AnalyzeText, "Cero detalles verificables (ni un plato, ni una fecha). Adjetivos genéricos y estructura de tres partes muy pulida.", true),
            F(InvestigationTool.AnalyzeProfile, "Cuenta creada hace 2 días con 48 reseñas, todas de 5 estrellas y con el mismo estilo.", true),
            F(InvestigationTool.CheckLink, "La reseña no contiene enlaces.", false));

        yield return Case("C-201", 2, ContentType.Email, "URGENTE: cuenta bloqueada",
            "De: soporte@banco-segur0.com",
            "Estimado cliente:\n\nURGENTE: Su cuenta bancaria será bloqueada hoy por actividad sospechosa. Para evitarlo, haga clic aquí y verifique su identidad en los próximos 30 minutos:\n\nhttps://bancoseguro-verificacion.co/login\n\nAtentamente,\nDepartamento de Seguridad",
            Verdict.Manipulated, Verdict.Manipulated,
            "Urgencia artificial y un dominio extraño. Suplantación: MANIPULADO.",
            Speaker.Ada, "Nivel de correos. Este mensaje llegó a cientos de empleados a la vez.",
            "Era MANIPULADO: un correo de phishing que suplanta a un banco. Pistas: urgencia y amenaza (\"30 minutos\"), remitente con un cero en lugar de una \"o\" y un enlace a un dominio recién registrado que no pertenece al banco. Nunca introduzcas credenciales desde un enlace recibido por correo.",
            true,
            F(InvestigationTool.ViewSource, "El remitente usa \"segur0\" con un CERO. El banco real usa otro dominio.", true),
            F(InvestigationTool.CheckLink, "bancoseguro-verificacion.co se registró hace 3 días y está en listas de phishing.", true),
            F(InvestigationTool.AnalyzeText, "Lenguaje de urgencia y amenaza, saludo genérico \"Estimado cliente\".", true),
            F(InvestigationTool.AnalyzeMetadata, "El servidor de envío está en un país distinto al del banco.", false));

        yield return Case("C-202", 2, ContentType.Email, "Cambio de horario del taller",
            "De: m.ramirez@universidad.edu.co",
            "Hola a todos,\n\nEl taller de pensamiento crítico del jueves pasa al salón 304 (bloque B) porque el auditorio está en mantenimiento. Empezamos a las 2:15 p. m., no a las 2:00, por el cambio de turno del bloque.\n\nTraigan el borrador del ensayo impreso, por favor.\n\nMarta Ramírez",
            Verdict.Human, Verdict.Human,
            "Detalles logísticos muy específicos y coherentes. Parece HUMANO.",
            Speaker.Ada, "Un correo institucional. Recuerda: estar bien escrito no lo convierte en IA.",
            "Era HUMANO. El correo tiene detalles concretos y verificables (salón, bloque, motivo del cambio de hora) y viene del dominio oficial de la universidad. Un texto correcto y formal no implica que sea generado por IA.",
            false,
            F(InvestigationTool.ViewSource, "Dominio oficial universidad.edu.co con firma digital válida.", true),
            F(InvestigationTool.AnalyzeText, "Información específica y coherente con el calendario académico publicado.", true),
            F(InvestigationTool.CheckLink, "No contiene enlaces.", false));

        yield return Case("C-301", 3, ContentType.Image, "Foto de perfil de una reclutadora",
            "Red profesional · \"Laura M., Talent Acquisition\"",
            "Laura te escribió ofreciéndote un trabajo remoto muy bien pagado y te pide tus datos personales. Esta es su foto de perfil.",
            Verdict.AI, Verdict.AI,
            "La imagen es demasiado perfecta y los textos del fondo no se leen. IA.",
            Speaker.Ada, "Nivel de imágenes. Observa los detalles, no solo la impresión general.",
            "Era IA. Los textos de las pantallas holográficas están deformados (\"DIAGNOTIC DATA\"), no hay datos de cámara y el perfil no tiene historial. Las imágenes generadas suelen fallar en texto, manos y detalles finos.",
            false,
            F(InvestigationTool.AnalyzeText, "Los textos de las pantallas del fondo son ilegibles o están mal escritos: típico de imágenes generadas.", true),
            F(InvestigationTool.AnalyzeMetadata, "Sin datos EXIF de cámara. Resolución 1408×768, habitual en generadores de imágenes.", true),
            F(InvestigationTool.AnalyzeProfile, "Perfil creado hace 1 semana, 500 contactos y ninguna publicación propia.", true));

        yield return Case("C-302", 3, ContentType.Image, "\"Foto filtrada\" de la sede de la DTA",
            "Publicado por @ph4ntom_leaks",
            "\"EXCLUSIVA: así es por dentro la agencia que decide qué es verdad. Foto tomada por un empleado arrepentido.\"",
            Verdict.AI, Verdict.AI,
            "El estilo parece una ilustración renderizada, no una foto. IA.",
            Speaker.Phantom, "¿Disfrutando tu primer día, analista? Te dejé un regalo. Veamos si sabes distinguir la realidad...",
            "Era IA. El nombre original del archivo revela el generador (\"Gemini_Generated_Image\"), la iluminación tiene estilo de ilustración y la cuenta que lo publica es de Phantom. Primera pista: Phantom usa IA para fabricar \"filtraciones\".",
            false,
            F(InvestigationTool.AnalyzeMetadata, "Nombre de archivo original: Gemini_Generated_Image_nzmm... El metadato delata el generador.", true),
            F(InvestigationTool.ViewSource, "La cuenta @ph4ntom_leaks se creó ayer y solo publica \"filtraciones\".", true),
            F(InvestigationTool.AnalyzeText, "El letrero del fondo tiene letras sin sentido.", false));

        yield return Case("C-401", 4, ContentType.SocialPost, "Titular viral sobre el agua",
            "Captura compartida 12.400 veces · atribuida a \"Diario El Observador\"",
            "\"CIENTÍFICOS CONFIRMAN: el agua del grifo de la ciudad causa pérdida de memoria\"\n\nComentario del usuario: ¡¡Compartan antes de que lo borren!!",
            Verdict.Manipulated, Verdict.Human,
            "El medio es real y conocido, así que la noticia debe ser real. HUMANO.",
            Speaker.Ada, "Nivel de redes sociales. Esta publicación se está volviendo viral.",
            "Era MANIPULADO. El medio existe, pero el titular original decía \"Científicos estudian la calidad del agua\". Alguien editó la captura (tipografías distintas) para alarmar. ¡Y ADA se equivocó! Que una fuente sea real no significa que el contenido no se haya alterado.",
            true,
            F(InvestigationTool.ViewSource, "En la web oficial del diario el titular es: \"Científicos estudian la calidad del agua de la ciudad\".", true),
            F(InvestigationTool.AnalyzeMetadata, "La captura fue editada: dos tipografías distintas en el mismo titular.", true),
            F(InvestigationTool.AnalyzeText, "Lenguaje alarmista y llamado urgente a compartir.", true),
            F(InvestigationTool.AnalyzeProfile, "Quien la publicó tiene 30 cuentas parecidas que comparten lo mismo.", false));

        yield return Case("C-402", 4, ContentType.SocialPost, "Cierre de vía por emergencia",
            "@BomberosCiudad · cuenta verificada desde 2012",
            "ALERTA: Cierre total de la Av. Circunvalar entre calles 45 y 52 por deslizamiento de tierra. Usen rutas alternas. Actualizaciones en bomberos.gov.co/emergencias",
            Verdict.Human, Verdict.Human,
            "Fuente oficial con historial largo. Parece HUMANO y fiable.",
            Speaker.Ada, "No todo es falso. Compartir información verificada también es responsable.",
            "Era HUMANO: una alerta real de una cuenta oficial verificada, con enlace a un dominio .gov.co y coincidente con otros medios. Compartir información verificada de emergencias ayuda a la comunidad.",
            true,
            F(InvestigationTool.AnalyzeProfile, "Cuenta oficial verificada desde 2012 con publicaciones constantes.", true),
            F(InvestigationTool.CheckLink, "bomberos.gov.co es el dominio oficial del gobierno.", true),
            F(InvestigationTool.ViewSource, "Dos medios locales confirman el cierre con fotos propias.", false));

        yield return Case("C-501", 5, ContentType.Audio, "Nota de voz del \"jefe\"",
            "Nota de voz · número desconocido (+57 3** *** 4410)",
            "[Transcripción del audio · 0:18]\n\"Hola, soy Carlos, tu jefe. Estoy en una reunión y no puedo hablar. Necesito que hagas ya una transferencia de 4 millones a la cuenta que te mando. Es urgente y confidencial, no se lo comentes a nadie.\"",
            Verdict.AI, Verdict.AI,
            "La voz suena como Carlos, pero la petición es sospechosa. Posible voz clonada por IA.",
            Speaker.Ada, "Nivel de audio. Ahora las voces también se pueden clonar.",
            "Era IA: una voz clonada. Pistas: número desconocido, urgencia y secreto, entonación plana sin respiraciones ni ruido de fondo. Ante peticiones de dinero, confirma siempre por otro canal (llama al número que ya conoces).",
            false,
            F(InvestigationTool.AnalyzeMetadata, "Audio sin ruido de fondo, sin respiraciones y con entonación demasiado uniforme.", true),
            F(InvestigationTool.ViewSource, "El número no está en la agenda de la empresa y se registró esta semana.", true),
            F(InvestigationTool.AnalyzeText, "Urgencia, secreto y petición de dinero: patrón clásico de fraude del CEO.", true));

        yield return Case("C-601", 6, ContentType.Video, "Video de un candidato",
            "Video viral · 2,1 M de reproducciones",
            "[Descripción del video · 0:42]\nUn candidato a la alcaldía aparece en un evento público diciendo: \"Sí, compramos votos y lo volveríamos a hacer\". El video circula a dos días de las elecciones.",
            Verdict.Manipulated, Verdict.AI,
            "Detecto rasgos sintéticos en la cara: todo el video es generado por IA.",
            Speaker.Phantom, "Las elecciones se deciden en las redes, analista. ¿Detendrás la verdad... o la mía?",
            "Era MANIPULADO: un deepfake construido sobre un video REAL de un discurso de 2019, con la cara y la voz alteradas. No es completamente generado por IA (como sugirió ADA): parte de material auténtico. Distinguir \"generado\" de \"manipulado\" importa.",
            true,
            F(InvestigationTool.AnalyzeMetadata, "El video se recomprimió 3 veces. El clip base es un discurso real de 2019 con otro audio.", true),
            F(InvestigationTool.AnalyzeText, "Los labios no coinciden con las palabras en varios momentos.", true),
            F(InvestigationTool.ViewSource, "Lo publicó primero una red de cuentas anónimas creadas este mes.", true),
            F(InvestigationTool.AnalyzeProfile, "El candidato denunció el video en su cuenta oficial.", false));

        yield return Case("C-701", 7, ContentType.Multimodal, "Operación Phantom",
            "Expediente DTA · evidencias relacionadas",
            "Texto: comunicado \"oficial\" que anuncia el cierre de la DTA por corrupción.\nImagen: foto de la directora de la DTA \"recibiendo sobornos\".\nPerfil: @DTA_Oficial_Prensa, 80.000 seguidores.\nAudio: llamada en la que la directora \"lo confiesa\".\nFuente: todo apareció a la misma hora en 300 cuentas.",
            Verdict.Manipulated, Verdict.Human,
            "Mis registros indican que todo es auténtico. HUMANO. (Datos posiblemente alterados)",
            Speaker.Phantom, "Último acto. Incluso tu querida ADA trabaja para mí ahora. ¿En quién confiarás?",
            "Era MANIPULADO: una campaña coordinada de Phantom que mezcla elementos reales y falsos. La foto real de la directora fue alterada, el audio es una voz clonada, el perfil es una suplantación y la publicación simultánea en 300 cuentas delata una red de bots. Phantom contaminó incluso los datos de ADA: por eso hay que verificar SIEMPRE.",
            true,
            F(InvestigationTool.AnalyzeProfile, "@DTA_Oficial_Prensa se creó hace 5 días; los seguidores son cuentas automatizadas.", true),
            F(InvestigationTool.AnalyzeMetadata, "La foto original (2024) muestra a la directora en un acto público: el sobre fue añadido.", true),
            F(InvestigationTool.ViewSource, "Publicado a la misma hora por 300 cuentas: patrón de red coordinada.", true),
            F(InvestigationTool.CheckLink, "El comunicado enlaza a dta-oficial.net, un dominio falso.", true),
            F(InvestigationTool.AnalyzeText, "El audio tiene cortes y la voz no respira entre frases.", true));
    }
}
