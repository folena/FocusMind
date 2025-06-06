using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ResultadoUIManager : MonoBehaviour
{
    [Header("Referência de UI")]
    public TextMeshPro textoResultado;

    private Dictionary<TesteManager.TipoTarefa, ResultadoFase> resultadosPorFase = new();

    public class ResultadoFase
    {
        public int acertos = 0;
        public int erros = 0;
        public int omissoes = 0;
        public int distratores = 0;
        public int letrasAlvo = 0;
        public float somaTempoReacao = 0f;
        public int totalReacoes = 0;

        public float MediaTempoReacao => totalReacoes > 0 ? somaTempoReacao / totalReacoes : 0f;
    }

    public void RegistrarAcerto(TesteManager.TipoTarefa fase, float tempoReacao)
    {
        var resultado = ObterResultado(fase);
        resultado.acertos++;
        resultado.somaTempoReacao += tempoReacao;
        resultado.totalReacoes++;
    }

    public void RegistrarErro(TesteManager.TipoTarefa fase)
    {
        ObterResultado(fase).erros++;
    }

    public void RegistrarOmissao(TesteManager.TipoTarefa fase)
    {
        ObterResultado(fase).omissoes++;
    }

    public void RegistrarDistrator(TesteManager.TipoTarefa fase)
    {
        ObterResultado(fase).distratores++;
    }

    public void RegistrarLetraAlvo(TesteManager.TipoTarefa fase)
    {
        ObterResultado(fase).letrasAlvo++;
    }

    private ResultadoFase ObterResultado(TesteManager.TipoTarefa fase)
    {
        if (!resultadosPorFase.ContainsKey(fase))
            resultadosPorFase[fase] = new ResultadoFase();

        return resultadosPorFase[fase];
    }

    public void MostrarResultadosNaTela()
    {
        string textoFinal = "<b>Resultados por Fase:</b>\n\n";

        foreach (var entrada in resultadosPorFase)
        {
            var fase = entrada.Key;
            var dados = entrada.Value;

            textoFinal += $"<b>{fase}</b>\n" +
                          $"Acertos: {dados.acertos}\n" +
                          $"Erros: {dados.erros}\n" +
                          $"Omissões: {dados.omissoes}\n" +
                          $"Letras-alvo apresentadas: {dados.letrasAlvo}\n" +
                          $"Tempo médio de reação: {dados.MediaTempoReacao:F2}s\n" +
                          $"Distratores olhados: {dados.distratores}\n\n";
        }

        textoResultado.text = textoFinal;
    }

    public void ResetarResultados()
    {
        resultadosPorFase.Clear();
        textoResultado.text = "";
    }
}
