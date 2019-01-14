var a,b : array[1..10] of array[1..20] of integer;
    i,j:integer;
begin
    for i := 1 to 10 do
        for j := 1 to 10 do
            a[i,j] := j*i;
            
    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(b[i,j], ' ');
        writeln;
    end;
    
    writeln;    
    
    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(a[i,j], ' ');
        writeln;
    end;
        
    writeln;    
    
    b[4] := a[4];
    b[1] := a[1];
    
    for i := 1 to 10 do 
        begin
            for j := 1 to 10 do
                write(b[i,j], ' ');
            writeln;
        end;
                
            
end.
