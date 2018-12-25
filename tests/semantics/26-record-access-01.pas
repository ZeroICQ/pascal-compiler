type
  film = record
    length : integer;
    rating: float;
  end;
                          

var a : integer;
    f : float;
    c : char;
    greenmile : film;   
begin
    greenmile.length := 102;
    greenmile.rating := 0.1e2;
    
    f := greenmile.rating;
    a := greenmile.length; 
end.