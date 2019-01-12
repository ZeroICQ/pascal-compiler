var 
    i : integer = -200 * 2;
    f : double = 10;
    mem : Array[20..25] of Integer;
    
begin
        if f > 100 then 
        begin
            i := 100;
            mem[21] := 21;
        end
        else begin
             i := 10;
             mem[21] := 21;
        
            if 2 + 2 > 10 then
                mem[25] := 25;
        end;
           
end.