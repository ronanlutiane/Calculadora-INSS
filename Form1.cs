using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Windows.Forms;

namespace Calculadora_INSS

{

    public partial class frmcalculadora : Form
    {

        // VARIAVEIS GLOBAIS DA CLASSE

        private decimal valorSalario = 0m;
        private int indice;
        // Definir as faixas de renda e as respectivas al�quotas (exemplo)
        private decimal[] faixas = { 1320.00m, 2571.29m, 3856.64m, 7507.49m };
        private double[] aliquotas = { 0.075, 0.09, 0.12, 0.14 };

        // Lista para armazenar os valores de contribui��o por faixa
        List<decimal> contribuicoesPorFaixa = new List<decimal>();

        // Lista para armazenar os sal�rios usados em cada faixa
        List<decimal> salariosPorFaixa = new List<decimal>();

        //int faixa = 0;
        decimal limiteFaixaAnterior = 0.0m;
        decimal contribuicaoFaixa = 0.0m;
        decimal salarioContribuicaoFaixa = 0.0m;
        double aliquotaFaixa = 0.0;
        //double salario = 0.0;
        decimal salarioBase = 0.0m;

        // FIM DAS VARIAVEIS GLOBAIS DA CLASSE 
        // ----------------------------------------------------------------------------------------------------------------------------------------------

        public frmcalculadora()
        {
            InitializeComponent();
        }

        private void frmCalculadora_Load(object sender, EventArgs e)
        {
            string[] periodo = { "A partir de Maio/2023", "At� Abril/2023" };
            foreach (string s in periodo) { cboPeriodo.Items.Add(s); }
        }

        private void txtValorSalario_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir apenas n�meros, ponto e v�rgula como entrada
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }


            // Se o separador decimal for v�rgula, substituir por ponto 
            if (e.KeyChar == ',')
            {
                e.KeyChar = '.';
            }

            // Se j� houver uma v�rgula na entrada, bloquear qualquer outro caractere v�rgula
            if (e.KeyChar == ',' && (sender as TextBox).Text.Contains(","))
            {
                e.Handled = true;
            }
        }


        private void txtValorSalario_TextChanged(object sender, EventArgs e)
        {
            valorSalario = decimal.Parse(txtValorSalario.Text, CultureInfo.InvariantCulture);
            salarioBase = valorSalario;
        }

        // Inicia a fun�ao original do programa para o calculo do INSS
        private void btnCalcular_Click(object sender, EventArgs e)
        {
            if (Validar())
            {
                RealizarCalculo();
            }
        }

        // Chama outras fun��es de valida��o espec�ficas e verifica se todas retornam true
        private bool Validar()
        {
            bool validarSalario = ValidarSalario();

            // Verifica se a valida��o do sal�rio falhou
            if (!validarSalario)
            {
                return false;
            }

            bool validarPeriodo = ValidarPeriodo();

            // Verifica se a valida��o do per�odo falhou
            if (!validarPeriodo)
            {
                return false;
            }

            // Se todas as valida��es espec�ficas foram bem-sucedidas, retorna true
            return true;
        }

        //Valida o preenchimento do salario. 
        private bool ValidarSalario()
        {
            if (valorSalario == 0)
            {
                MessageBox.Show("O valor do sal�rio base n�o pode ser 0. Informe um novo valor!");
                txtValorSalario.Focus();
                return false;

            }
            else
            {
                return true;
            }
        }

        //Valida a escolha de per�odo.
        private bool ValidarPeriodo()
        {
            if (indice == -1)
            {
                MessageBox.Show("O per�odo n�o pode ser nulo, selecione um per�odo!");
                cboPeriodo.Focus();
                return false;
            }
            else
            {
                return true;
            }
        }

        //Chama o calculo de acordo com a op��o do combobox. 
        private void RealizarCalculo()
        {
            switch (indice)
            {
                case 0:
                    // Chama o c�lculo
                    CalculaINSS resultado = CalculaInssNovo(valorSalario);

                    // Acessar os valores do c�lculo
                    decimal contribuicaoTotal = resultado.ContribuicaoTotal;
                    List<decimal> contribuicoesPorFaixa = resultado.ContribuicoesPorFaixa;
                    List<decimal> salariosPorFaixa = resultado.SalariosPorFaixa;

                    // Chama a fun��o para montar a mensagem
                    string mensagem = MontarMensagem(contribuicaoTotal, contribuicoesPorFaixa, salariosPorFaixa);

                    MessageBox.Show(mensagem);
                    break;
                case 1:
                    CalculaInssAntigo(valorSalario);
                    break;
            }
        }

        private void cboPeriodo_SelectedIndexChanged(object sender, EventArgs e)
        {
            indice = cboPeriodo.SelectedIndex;
        }

        //Calcula o INSS com base na regra de tabela progressiva por faixa de renda.
        public CalculaINSS CalculaInssNovo(decimal salario)
        {
            // Inicializar o valor total da contribui��o
            decimal contribuicaoTotal = 0.0m;

            // Verificar e limpar as listas no in�cio do c�lculo
            if (contribuicoesPorFaixa.Count > 0 || salariosPorFaixa.Count > 0)
            {
                LimparListas();
            }

            // Tratamento para os casos em que o salario � maior que o teto, de forma a nao permitir recolhimentos maiores que o teto. 
            if (salario > faixas[3])
            {
                decimal teste = faixas[3];
                salario = faixas[3];
            }

            // Verificar se o sal�rio � menor ou igual ao limite da primeira faixa e calcula a contribui�ao para esse caso. 
            if (salario <= faixas[0])
            {
                decimal contribuicaoFaixa = salario * (decimal)aliquotas[0];
                contribuicoesPorFaixa.Add(contribuicaoFaixa);
                salariosPorFaixa.Add(salario);
                contribuicaoTotal += contribuicaoFaixa;
            }
            else
            {
                // Se o salario nao � menor ou igual ao limite da primeira faixa, determina em qual faixa limite ele vai se encaixar 
                int faixa = 0;
                decimal limiteFaixaAnterior = 0.0m;
                decimal salarioBase = salario;
                while (faixa < faixas.Length && salario > faixas[faixa])
                {
                    faixa++;
                }

                // Calcula o valor de cada faixa pela qual o salario passa com base no numero de faixas que ele se encaixou no passo anterior. 
                for (int i = 0; i <= faixa; i++)
                {

                    // Primeira faixa salarial
                    if (i == 0)
                    {
                        CalculaINSSPrimeiraFaixa(i, ref contribuicaoTotal, ref salario);
                    }
                    // Segunda faixa salarial
                    else if (salarioBase > faixas[i] && salario - limiteFaixaAnterior > faixas[i])
                    {
                        CalculaINSSSegundaFaixa(i, ref contribuicaoTotal, ref salario);
                    }
                    // Terceira faixa salarial
                    else if (salarioBase > faixas[i] && salario - limiteFaixaAnterior <= faixas[i])
                    {
                        CalculaINSSTerceiraFaixa(i, ref contribuicaoTotal, ref salario);
                    }
                    // �ltima faixa salarial
                    else
                    {
                        /*
                        // Calcula a contribui��o da faixa atual usando o residuo entre o salario e a faixa anterior
                        aliquotaFaixa = aliquotas[i]; // Apenas para depura�ao
                        double contribuicaoUltimaFaixa = salario * aliquotas[i];

                        contribuicoesPorFaixa.Add(contribuicaoUltimaFaixa);
                        salariosPorFaixa.Add(salario); // Subtrai o limite da faixa anterior
                        contribuicaoTotal += contribuicaoUltimaFaixa;
                        limiteFaixaAnterior = faixas[i];
                        salario -= limiteFaixaAnterior;

                        // Reinicia as variaveis para utiliza�ao nos calculos seguintes. 
                        aliquotaFaixa = 0.0;
                        contribuicaoFaixa = 0.0;*/
                        CalculaINSSResidual(i, ref contribuicaoTotal, ref salario);
                    }
                    if (salario <= 0) break;
                }
            }

            // Criar uma inst�ncia do resultado e atribuir os valores
            return new CalculaINSS
            {
                ContribuicaoTotal = contribuicaoTotal,
                ContribuicoesPorFaixa = contribuicoesPorFaixa,
                SalariosPorFaixa = salariosPorFaixa
            };
        }



        //Calcula o INSS com base na regra antiga. 
        private void CalculaInssAntigo(decimal salario)
        {

        }
        // Calcula o INSS para a primeira faixa.
        public void CalculaINSSPrimeiraFaixa(int i, ref decimal contribuicaoTotal, ref decimal salario)
        {

            aliquotaFaixa = aliquotas[i]; // Apenas para depura�ao
            contribuicaoFaixa = faixas[i] * (decimal)aliquotas[i];
            contribuicoesPorFaixa.Add(contribuicaoFaixa);
            contribuicaoTotal += contribuicaoFaixa;
            salarioContribuicaoFaixa = faixas[i]; // Apenas para depura�ao
            salariosPorFaixa.Add(faixas[i]);

            // Define o limite da faixa anterior como o atual para continuar os calculos. . 
            limiteFaixaAnterior = faixas[i];
            salario -= limiteFaixaAnterior;

            // Reinicia as variaveis para utiliza�ao nos calculos seguintes. 
            ReiniciarVariaveis();
        }

        public void CalculaINSSSegundaFaixa(int i, ref decimal contribuicaoTotal, ref decimal salario)
        {

            aliquotaFaixa = aliquotas[i]; // Apenas para depura�ao
            contribuicaoFaixa = (faixas[i] - limiteFaixaAnterior) * (decimal)aliquotas[i];
            contribuicoesPorFaixa.Add(contribuicaoFaixa);
            contribuicaoTotal += contribuicaoFaixa;

            // Define qual o salario de contribui�ao para a faixa atual e armazena. 
            salarioContribuicaoFaixa = faixas[i] - limiteFaixaAnterior;
            salariosPorFaixa.Add(salarioContribuicaoFaixa);
            // Atualiza o limite da faixa anterior para continuar os c�lculos. 
            limiteFaixaAnterior = faixas[i];
            salario -= limiteFaixaAnterior;

            // Reinicia as variaveis para utiliza�ao nos calculos seguintes. 
            ReiniciarVariaveis();
        }

        public void CalculaINSSTerceiraFaixa(int i, ref decimal contribuicaoTotal, ref decimal salario)
        {

            aliquotaFaixa = aliquotas[i]; // Apenas para depura�ao
            contribuicaoFaixa = (faixas[i] - limiteFaixaAnterior) * (decimal)aliquotas[i];
            contribuicoesPorFaixa.Add(contribuicaoFaixa);
            contribuicaoTotal += contribuicaoFaixa;
            salariosPorFaixa.Add(faixas[i] - limiteFaixaAnterior);
            // Recebe o limite da faixa anterior para calcular o residuo. 
            limiteFaixaAnterior = faixas[i];
            salario = salarioBase;
            if (salario > faixas[3])
            {
                salario = faixas[3];
            }
            salario -= limiteFaixaAnterior;

            // Reinicia as variaveis para utiliza�ao nos calculos seguintes. 
            ReiniciarVariaveis();
        }

        public void CalculaINSSResidual(int i, ref decimal contribuicaoTotal, ref decimal salario)
        {

            // Calcula a contribui��o da faixa atual usando o residuo entre o salario e a faixa anterior
            aliquotaFaixa = aliquotas[i]; // Apenas para depura�ao
            decimal contribuicaoUltimaFaixa = salario * (decimal)aliquotas[i];

            contribuicoesPorFaixa.Add(contribuicaoUltimaFaixa);
            salariosPorFaixa.Add(salario); // Subtrai o limite da faixa anterior
            contribuicaoTotal += contribuicaoUltimaFaixa;
            limiteFaixaAnterior = faixas[i];
            salario -= limiteFaixaAnterior;

            // Reinicia as variaveis para utiliza�ao nos calculos seguintes. 
            ReiniciarVariaveis();
        }

        public void ReiniciarVariaveis()
        {

            aliquotaFaixa = 0.0;
            contribuicaoFaixa = 0.0m;
            salarioContribuicaoFaixa = 0.0m;
        }



        //Monta a mensagem com os dados do processamento que sera exibida ao usuario. 
        private string MontarMensagem(decimal contribuicaoTotal, List<decimal> contribuicoesPorFaixa, List<decimal> salariosPorFaixa)
        {
            string mensagem = $"O valor do INSS Calculado foi de: R$ {contribuicaoTotal} \n\n"; // Alterado de concatena�ao de strings para interpola��o por se tratar de melhor pr�tica. 
            mensagem += "Contribui��es por Faixa:\n"; // Testar e ver se essa linha ainda funciona, se nao, mudar para interpola��o como acima. 
            for (int i = 0; i < contribuicoesPorFaixa.Count; i++)
            {
                //mensagem += "Faixa " + (i + 1) + ": R$ " + contribuicoesPorFaixa[i] + "\n"; 
                // Alterado de concatena�ao de strings para interpola��o por se tratar de melhor pr�tica. Usada formata��o de moeda. 
                mensagem += $"Faixa {i +1} : {contribuicoesPorFaixa[i]:C}\n"; 
            }
            mensagem += "\nSal�rios por Faixa:\n";
            for (int i = 0; i < salariosPorFaixa.Count; i++)
            {
                // mensagem += "Faixa " + (i + 1) + ": R$ " + salariosPorFaixa[i] + "\n"; 
                // Alterado de concatena�ao de strings para interpola��o por se tratar de melhor pr�tica. Usada formata��o de moeda. 
                mensagem += $"Faixa {i + 1} {salariosPorFaixa[i]:C}\n";
            }
            return mensagem;
        }

        //Destruindo objetos usados no programa
        private void LimparTudo()
        {
            // Limpar todas as listas ou objetos que precisam ser limpos
            LimparListas();
            // Outros objetos a serem limpos, se houver

            // Reinicializar vari�veis ou objetos, se necess�rio
            ReiniciarVariaveis();
        }

        //Limpar as listas usadas para armazenar os dados do programa
        private void LimparListas()
        {
            contribuicoesPorFaixa.Clear();
            salariosPorFaixa.Clear();
        }

    }



    //Classe para devolver os resultados ao programa principal. 
    public class CalculaINSS
    {
        public decimal ContribuicaoTotal { get; set; }
        public List<decimal> ContribuicoesPorFaixa { get; set; }
        public List<decimal> SalariosPorFaixa { get; set; }
    }

}