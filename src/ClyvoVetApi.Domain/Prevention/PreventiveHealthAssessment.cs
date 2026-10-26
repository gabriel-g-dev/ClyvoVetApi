using ClyvoVetApi.Models;

namespace ClyvoVetApi.Prevention;

public sealed record PreventiveHealthResult(int Score, string Status, IReadOnlyList<string> Recomendacoes);

public static class PreventiveHealthAssessment
{
    public static PreventiveHealthResult Evaluate(Pet pet, DateTime today)
    {
        int score = 100;
        var recomendacoes = new List<string>();

        int vacinasAtrasadas = 0;
        int vacinasPendentes = 0;

        foreach (var vacina in pet.Vacinas)
        {
            if (vacina.Status == "P")
            {
                if (vacina.Data < today)
                {
                    vacinasAtrasadas++;
                    score -= 15;
                }
                else if (vacina.Data <= today.AddDays(7))
                {
                    vacinasPendentes++;
                    score -= 5;
                }
            }
        }

        var consultasRealizadas = pet.Consultas.Where(c => c.Status == "R").ToList();
        if (pet.Consultas.Count == 0)
        {
            score -= 20;
            recomendacoes.Add("🏥 Nenhuma consulta preventiva foi registrada ainda. Recomendamos agendar uma consulta inicial com o veterinário.");
        }
        else if (consultasRealizadas.Count == 0)
        {
            score -= 20;
            recomendacoes.Add("🏥 Nenhuma consulta foi realizada ainda. Recomendamos comparecer a uma consulta de rotina.");
        }
        else
        {
            var ultimaConsulta = consultasRealizadas.Max(c => c.Data);
            if (ultimaConsulta < today.AddMonths(-6))
            {
                score -= 10;
                recomendacoes.Add("🩺 Já faz mais de 6 meses desde a última consulta de rotina. É altamente recomendável agendar um check-up periódico.");
            }
        }

        score = Math.Clamp(score, 0, 100);

        string status = score switch
        {
            >= 80 => "EXCELENTE",
            >= 50 => "ATENÇÃO",
            _ => "CRÍTICO"
        };

        if (score == 100)
        {
            recomendacoes.Add("🌟 Excelente! O score de saúde preventiva de seu pet está perfeito! Continue cuidando dele com tanto carinho.");
        }
        else
        {
            if (vacinasAtrasadas > 0)
            {
                recomendacoes.Add($"💉 Há {vacinasAtrasadas} vacina(s) em atraso! Recomendamos agendar a aplicação com urgência para manter a imunização ativa.");
            }
            if (vacinasPendentes > 0)
            {
                recomendacoes.Add($"📅 Há {vacinasPendentes} vacina(s) agendada(s) para os próximos dias. Não se esqueça de comparecer!");
            }
            if (score < 50)
            {
                recomendacoes.Add("🚨 Atenção redobrada: O status de saúde do seu pet está CRÍTICO. Recomendamos entrar em contato imediatamente com o seu veterinário de confiança.");
            }
        }

        recomendacoes.Add("🍎 Dica extra: Mantenha sempre água fresca disponível, ração de qualidade e estimule passeios diários para manter o pet ativo e saudável.");

        return new PreventiveHealthResult(score, status, recomendacoes);
    }
}
