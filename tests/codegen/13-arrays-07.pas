var a,b : array[1..10] of array[1..20] of array[1..100] of  char;
    i,j,k :integer;
begin
    for i := 1 to 10 do
        for j := 1 to 10 do
            for k := 1 to 100 do
                if (j+k+i div 2) <> 0 then
                    a[i,j,k] := 'z'
                else
                    a[i,j,k] := 'a';
            
    for i := 1 to 10 do 
        for j := 1 to 10 do
            for k := 1 to 100 do
                writeln(a[i,j,k], ' ');
    
    for i := 1 to 10 do 
        for j := 1 to 10 do
            for k := 1 to 100 do
                writeln(b[i,j,k], ' ');

    b[2,3] := a[4,5];
    b[4,6,7] := a[8,10,70];
    b[1] := a[9];
    
    
    for i := 1 to 10 do 
        for j := 1 to 10 do
            for k := 1 to 100 do
                writeln(a[i,j,k], ' ');
    
    for i := 1 to 10 do 
        for j := 1 to 10 do
            for k := 1 to 100 do
                writeln(b[i,j,k], ' ');
            
end.
