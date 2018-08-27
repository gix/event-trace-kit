Imports System.Diagnostics.Tracing
Imports System.Threading

Module Module1
    Sub Main()
        Dim es = New MinimalEventSource()

        For i = 0 To 50
            es.EventWrite(i)
            Console.WriteLine("DotNetConsoleApp {0}", i)
            Thread.Sleep(1000)
        Next
    End Sub

    <EventSource(Guid:="{5AB0948E-C045-411A-AC12-AC455AFA8DF2}")>
    Class MinimalEventSource
        Inherits EventSource

        <[Event](1)>
        Public Sub EventWrite(id As Integer)
            WriteEvent(1, id)
        End Sub
    End Class
End Module
