type 
    myarray = type array[22..22] of integer;
    meh = type myarray;
    
var
    i : myarray;
    b : array[22..22] of integer;
    c : meh;
begin
    i := b;
    i := c;
    i := i;
    
    b := b;
    b := c;
    b := i;
    
    c := b;
    c := c;
    c := i;
end.