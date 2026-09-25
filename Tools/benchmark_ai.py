from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[1]
temporary=root/'.utmp/benchmark'; temporary.mkdir(exist_ok=True)
baseline=subprocess.check_output(['git','show','HEAD:Assets/Scripts/Hard/AiBoard.cs'],cwd=root,text=True).replace('AIBoard','BaselineAIBoard')
(temporary/'Baseline.cs').write_text(baseline,encoding='utf-8')
(temporary/'Bench.cs').write_text('''using System;
using System.Diagnostics;
using System.Threading;
class Bench {
 static void Main() {
 var state=new PieceType[7,6];
 var old=new BaselineAIBoard(state); var current=new AIBoard(state);
 for(int i=0;i<10;i++){old.EvaluateBoard();current.EvaluateBoard();}
 Run("baseline",()=>old.EvaluateBoard()); Run("updated",()=>current.EvaluateBoard());
 var rng=new Random(42);
 for(int sample=0;sample<100;sample++) {
  var model=new AIBoard(new PieceType[7,6]); var original=new BaselineAIBoard(new PieceType[7,6]);
  for(int m=0;m<sample%25;m++) { int c=rng.Next(7); if(model.IsValidMove(c)){var p=m%2==0?PieceType.Player1:PieceType.Player2;model.MakeMove(c,p);original.MakeMove(c,p);} }
  if(model.EvaluateBoard()!=original.EvaluateBoard()) throw new Exception("Evaluator changed");
 }
 Console.WriteLine("PASS: 100 seeded board evaluations preserve heuristic scores");
 var watch=Stopwatch.StartNew(); int move=AISearch.FindBestMove(state,CancellationToken.None);watch.Stop();
 Console.WriteLine("Hard opening search: "+watch.Elapsed.TotalMilliseconds+" ms; column "+move);
 }
 static void Run(string name,Func<int> eval){
 var watch=new Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();watch.Start();
 for(int i=0;i<1000;i++)eval();watch.Stop();long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
 Console.WriteLine(name+": 1000 evaluations; "+watch.Elapsed.TotalMilliseconds+" ms; "+bytes+" bytes");
 }
}''',encoding='utf-8')
rsp=next((root/'Library/Bee/artifacts').glob('*E.dag/Assembly-CSharp.rsp')).read_text(encoding='utf-8-sig')
lines=[x for x in rsp.splitlines() if not x.strip('"').endswith('.cs') and not x.startswith(('-out:','-refout:','-target:','-analyzer:'))]
lines+=['-target:exe','-out:".utmp/benchmark/Bench.exe"','".utmp/benchmark/Baseline.cs"','".utmp/benchmark/Bench.cs"','"Assets/Scripts/Gameplay/PieceType.cs"','"Assets/Scripts/Hard/AiBoard.cs"','"Assets/Scripts/Hard/AISearch.cs"']
(temporary/'bench.rsp').write_text('\n'.join(lines),encoding='utf-8')
editor=Path('C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data')
subprocess.run(['dotnet',str(editor/'DotNetSdkRoslyn/csc.dll'),'@.utmp/benchmark/bench.rsp'],cwd=root,check=True)
result=subprocess.run([str(editor/'MonoBleedingEdge/bin/mono.exe'),str(temporary/'Bench.exe')],cwd=root,check=True,capture_output=True,text=True)
(root/'Documentation/Validation').mkdir(exist_ok=True)
(root/'Documentation/Validation/ai-benchmark.txt').write_text(result.stdout,encoding='utf-8')
print(result.stdout)
