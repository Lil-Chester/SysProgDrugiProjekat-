Zadatak 35:
Kreirati Web server koji vrši brojanje reči u okviru fajla koje su anagrami zadate reči. Svi zahtevi
serveru se šalju preko browser-a korišćenjem GET metode. U zahtevu se kao parametri navode
naziv fajla i zadata reč. Server prihvata zahtev, pretražuje root folder i sve njegove podfoldere za
zahtevani fajl i vrši brojanje reči koje su anagrami (sadrže ista slova kao zadata reč, ali u
drugačijem redosledu). Ukoliko traženi fajl ne postoji, vratiti grešku korisniku. Takođe, ukoliko
nema reči koje su anagrami, vratiti odgovarajuću poruku korisniku.
Primer poziva serveru: http://localhost:5050/?fajl=fajl.txt&rec=sako
