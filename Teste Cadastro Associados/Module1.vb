Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Data.Entity
Imports System.Runtime.Remoting.Contexts

' Modelos de dados
Public Class Associado
    Public Property Id As Integer
    Public Property Nome As String
    Public Property Cpf As String
    Public Property DataNascimento As DateTime
    Public Overridable Property Empresas As ICollection(Of Empresa) = New HashSet(Of Empresa)
End Class

Public Class Empresa
    Public Property Id As Integer
    Public Property Nome As String
    Public Property Cnpj As String
    Public Overridable Property Associados As ICollection(Of Associado) = New HashSet(Of Associado)
End Class

' Contexto do Banco de Dados
Public Class ApplicationContext
    Inherits DbContext
    Public Property Associados As DbSet(Of Associado)
    Public Property Empresas As DbSet(Of Empresa)

    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        ' Configuração do relacionamento N para N
        modelBuilder.Entity(Of Associado)().
            HasMany(Function(a) a.Empresas).
            WithMany(Function(e) e.Associados).
            Map(Sub(mapping)
                    mapping.MapLeftKey("AssociadoId")
                    mapping.MapRightKey("EmpresaId")
                    mapping.ToTable("AssociadoEmpresa")
                End Sub)
    End Sub
End Class

' Repositório para manipulação de dados
Public Class Repositorio
    Public Shared Function AdicionarAssociado(associado As Associado, empresasIds As List(Of Integer)) As Integer
        Using context As New ApplicationContext()
            ' Verifica se o CPF é único
            If context.Associados.Any(Function(a) a.Cpf = associado.Cpf) Then
                Throw New Exception("CPF já cadastrado.")
            End If

            ' Adiciona as empresas ao associado
            associado.Empresas = context.Empresas.Where(Function(e) empresasIds.Contains(e.Id)).ToList()

            ' Adiciona e salva as alterações
            context.Associados.Add(associado)
            context.SaveChanges()
            Return associado.Id
        End Using
    End Function

    Public Shared Sub AtualizarAssociado(associado As Associado, novasEmpresasIds As List(Of Integer))
        Using context As New ApplicationContext()
            ' Verifica se o CPF é único
            If context.Associados.Any(Function(a) a.Cpf = associado.Cpf And a.Id <> associado.Id) Then
                Throw New Exception("CPF já cadastrado.")
            End If

            ' Carrega o associado do banco de dados com as empresas
            Dim associadoDoBanco = context.Associados.
                Include(Function(a) a.Empresas).
                SingleOrDefault(Function(a) a.Id = associado.Id)

            If associadoDoBanco IsNot Nothing Then
                ' Atualiza os dados do associado
                context.Entry(associadoDoBanco).CurrentValues.SetValues(associado)

                ' Atualiza as empresas vinculadas ao associado
                associadoDoBanco.Empresas.Clear()
                associadoDoBanco.Empresas = context.Empresas.Where(Function(e) novasEmpresasIds.Contains(e.Id)).ToList()

                ' Salva as alterações
                context.SaveChanges()
            End If
        End Using
    End Sub

    Public Shared Function ConsultarAssociadosPorNome(nome As String) As List(Of Associado)
        Using context As New ApplicationContext()
            Return context.Associados.Where(Function(a) a.Nome.Contains(nome)).ToList()
        End Using
    End Function

    Public Shared Sub ExcluirAssociado(id As Integer)
        Using context As New ApplicationContext()
            ' Carrega o associado do banco de dados com as empresas
            Dim associadoDoBanco = context.Associados.
                Include(Function(a) a.Empresas).
                SingleOrDefault(Function(a) a.Id = id)

            If associadoDoBanco IsNot Nothing Then
                ' Remove as associações com empresas
                associadoDoBanco.Empresas.Clear()

                ' Remove o associado
                context.Associados.Remove(associadoDoBanco)

                ' Salva as alterações
                context.SaveChanges()
            End If
        End Using
    End Sub

    Public Shared Function ConsultarEmpresaPorId(id As Integer) As Empresa
        Using context As New ApplicationContext()
            Return context.Empresas.Find(id)
        End Using
    End Function

    Public Shared Function ConsultarAssociadoPorId(id As Integer) As Associado
        Using context As New ApplicationContext()
            Return context.Associados.Find(id)
        End Using
    End Function

    Public Shared Function ConsultarTodasEmpresas() As List(Of Empresa)
        Using context As New ApplicationContext()
            Return context.Empresas.ToList()
        End Using
    End Function

    Public Shared Function ConsultarTodosAssociados() As List(Of Associado)
        Using context As New ApplicationContext()
            Return context.Associados.ToList()
        End Using
    End Function

    Public Shared Sub ExcluirEmpresa(id As Integer)
        Using context As New ApplicationContext()
            Dim empresaDoBanco = context.Empresas.Find(id)

            If empresaDoBanco IsNot Nothing Then
                context.Empresas.Remove(empresaDoBanco)
                context.SaveChanges()
            End If
        End Using
    End Sub

    Public Shared Sub AtualizarEmpresa(empresaConsultada As Empresa, novosAssociadosIds As List(Of Integer))
        Using context As New ApplicationContext()
            Dim empresaDoBanco = context.Empresas.Find(empresaConsultada.Id)

            If empresaDoBanco IsNot Nothing Then
                context.Entry(empresaDoBanco).CurrentValues.SetValues(empresaConsultada)

                empresaDoBanco.Associados.Clear()
                empresaDoBanco.Associados = context.Associados.Where(Function(a) novosAssociadosIds.Contains(a.Id)).ToList()

                context.SaveChanges()
            End If
        End Using
    End Sub

    Friend Shared Function ConsultarEmpresasPorNome(nome As String) As List(Of Empresa)
        Using context As New ApplicationContext()
            Return context.Empresas.Where(Function(e) e.Nome.Contains(nome)).ToList()
        End Using
    End Function


    ' Métodos semelhantes para as operações de empresa
End Class
Module Module1
    Sub Main()
        ' Exemplo de uso
        Dim novoAssociado As New Associado With {
            .Nome = "João da Silva",
            .Cpf = "12345678901",
            .DataNascimento = New DateTime(1990, 1, 1)
        }

        ' Adiciona uma empresa ao banco de dados para associar ao associado
        Dim novaEmpresa As New Empresa With {
            .Nome = "Empresa A",
            .Cnpj = "12345678901234"
        }

        Using context As New ApplicationContext()
            context.Empresas.Add(novaEmpresa)
            context.SaveChanges()
        End Using

        Dim associadoId = Repositorio.AdicionarAssociado(novoAssociado, New List(Of Integer) From {novaEmpresa.Id})

        ' Consulta associados por nome
        Dim associadosConsultados = Repositorio.ConsultarAssociadosPorNome("João")

        If associadosConsultados.Count > 0 Then
            Dim associadoConsultado = associadosConsultados.First()

            ' Atualiza o associado e adiciona outra empresa
            associadoConsultado.Nome = "João da Silva Santos"
            Dim outraEmpresa As New Empresa With {
                .Nome = "Empresa B",
                .Cnpj = "56789012345678"
            }

            Using context As New ApplicationContext()
                context.Empresas.Add(outraEmpresa)
                context.SaveChanges()
            End Using

            Repositorio.AtualizarAssociado(associadoConsultado, New List(Of Integer) From {outraEmpresa.Id})
        End If

        ' Exclui associado
        Repositorio.ExcluirAssociado(associadoId)

        ' Adiciona uma nova empresa
        Dim novaEmpresa2 As New Empresa With {
            .Nome = "Empresa C",
            .Cnpj = "98765432101234"
        }

        Using context As New ApplicationContext()
            context.Empresas.Add(novaEmpresa2)
            context.SaveChanges()
        End Using

        ' Consulta empresas por nome
        Dim empresasConsultadas = Repositorio.ConsultarEmpresasPorNome("Empresa")

        If empresasConsultadas.Count > 0 Then
            Dim empresaConsultada = empresasConsultadas.First()

            ' Atualiza a empresa e adiciona outro associado
            empresaConsultada.Nome = "Empresa D"
            Dim outroAssociado As New Associado With {
                .Nome = "Maria Oliveira",
                .Cpf = "98765432109",
                .DataNascimento = New DateTime(1985, 5, 15)
            }

            Using context As New ApplicationContext()
                context.Associados.Add(outroAssociado)
                context.SaveChanges()
            End Using

            Repositorio.AtualizarEmpresa(empresaConsultada, New List(Of Integer) From {outroAssociado.Id})
        End If

        ' Exclui empresa
        Repositorio.ExcluirEmpresa(novaEmpresa2.Id)

        ' Exibe todas as empresas
        Dim todasEmpresas = Repositorio.ConsultarTodasEmpresas()
        Console.WriteLine("Todas as empresas:")
        For Each empresa In todasEmpresas
            Console.WriteLine($"{empresa.Id} - {empresa.Nome} - {empresa.Cnpj}")
        Next

        ' Exibe todas os associados
        Dim todosAssociados = Repositorio.ConsultarTodosAssociados()
        Console.WriteLine("Todos os associados:")
        For Each associado In todosAssociados
            Console.WriteLine($"{associado.Id} - {associado.Nome} - {associado.Cpf}")
        Next

        Console.ReadLine()
    End Sub

End Module
