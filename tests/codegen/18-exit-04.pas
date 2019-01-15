type
    pmi = record
        mark:char; 
        age:integer;
        students: array[2..6] of array[2..9] of char;
    end;

function foo(): pmi;
var meh: pmi;
    i, j : integer;
begin

    meh.mark := 'f';
    meh.age  := 129;
    for i := 2 to 6 do
        for j := 2 to 9 do
            meh.students[i,j] := 'm';
            
  exit(meh);
end;


var kek : pmi;
var i, j : integer;
begin
    writeln(kek.mark);
    writeln(kek.age);
    
    for i := 2 to 6 do
        for j := 2 to 9 do
            writeln(kek.students[i,j]);
            
    kek := foo();
            
    writeln(kek.mark);
    writeln(kek.age);
    
    for i := 2 to 6 do
        for j := 2 to 9 do
            writeln(kek.students[i,j]);
end.