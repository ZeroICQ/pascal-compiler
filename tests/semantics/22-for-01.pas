var i:integer;
 f: double = 1.2e100;
 
begin
    
    for i:=2 to 10 do
        f -= 1;
        
    for i := 200 downto 0 do
    begin
        f -= 1;
        f += 1;
    end;
    
end.