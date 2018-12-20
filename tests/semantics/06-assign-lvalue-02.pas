const
    i = 29;
    
var 
    mem : Array[20..25] of Integer;
    
begin
//fpc by default have no compile time check for const expr index; 
    mem[i] := 23;
end.