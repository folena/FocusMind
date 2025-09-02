# MindTarget – Avaliação de Atenção em Realidade Virtual

## Sobre o Projeto
O **MindTarget** é um protótipo desenvolvido em **Unity** com integração ao **Oculus Quest 2**, criado como parte do Trabalho de Conclusão de Curso.  
Seu objetivo é **auxiliar psicólogos e psiquiatras na identificação de sinais de TDAH em crianças de 10 a 16 anos**, oferecendo uma abordagem imersiva, lúdica e padronizada.

O sistema é inspirado no **TAVIS-4**, um protocolo reconhecido de avaliação neuropsicológica da atenção, e adapta suas fases para o ambiente de **Realidade Virtual**.

---

## Funcionalidades
- Simulação de **três fases de testes de atenção**:
  - Atenção **Concentrada**
  - Atenção **Alternada**
  - Atenção **Dividida**
- Registro de métricas:
  - Acertos e Erros
  - Omissões
  - Tempo de reação médio
  - Interações com distratores
  - Quantidade de letras-alvo apresentadas
- Distratores auditivos programados (ex: celular tocando, alarme, risada).
- Interface amigável e gamificada para crianças.

---

## Tecnologias Utilizadas
- **Unity 2022+** (C#)
- **Oculus Quest 2** (XR Interaction Toolkit)
- **TextMeshPro** para interface
- Scripts organizados em:
  - `StimulusSpawner` → controle dos estímulos visuais
  - `DistratorController` → controle dos distratores sonoros
  - `TesteManager` → gerência de fases e temporização
  - `ResultadoUIManager` → coleta e exibição dos resultados

---

## Resultados
Ao final do teste, o sistema exibe em tela:
- Dados quantitativos por fase
- Tempo médio de reação
- Interações com distratores
- Estatísticas de desempenho

Essas informações servirão de base para **apoio ao diagnóstico clínico**.

---

## 👨‍💻 Autoria
Desenvolvido por **Marcos Vinicius Folena**, como parte do TCC em Engenharia da Computação – Facens.  

Orientação: **Fabio Rodrigo Colombini**

---

## 📜 Licença
Este projeto é acadêmico e de uso não-comercial.
