using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las (można ściąć drzewo)
        public const int LAKA = 2;    // łąka (pusty teren)
        public const int SKALA = 3;   // skały (blokują ruch i zabierają energię)
        public const int JAGODY = 4;  // jagody (regenerują energię)
        public const int ILE_TERENOW = 5; // Liczba typów terenu

        // Zmienne przechowujące stan gry
        private int[,] mapa; // Dwuwymiarowa tablica reprezentująca mapę
        private int szerokoscMapy; // Szerokość mapy w segmentach
        private int wysokoscMapy;  // Wysokość mapy w segmentach
        private Image[,] tablicaTerenu; // Tablica obrazków terenu
        private const int RozmiarSegmentu = 32; // Rozmiar pojedynczego kafelka w pikselach
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW]; // Ładowane tekstury

        // Zmienne związane z graczem
        private int pozycjaGraczaX = 0; // Aktualna pozycja X gracza
        private int pozycjaGraczaY = 0; // Aktualna pozycja Y gracza
        private Image obrazGracza; // Obrazek reprezentujący gracza
        private int iloscDrewna = 0; // Zebrane drewno
        private int energia = 100; // Poziom energii gracza
        private DispatcherTimer timerEnergii; // Timer do zmniejszania energii
        private int liczbaRund = 1; // Aktualna runda
        private int liczbaDrzewNaMapie = 0; // Liczba drzew na mapie

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu(); // Ładuje tekstury z dysku

            // Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("Zdjecia/gracz.png", UriKind.Relative))
            };

            StartTimerEnergii(); // Uruchamia odliczanie energii
            GenerujLosowaMape(20, 15); // Tworzy nową mapę 20x15
        }

        // Animacja zmiany rundy (miganie tekstu)
        private void AnimujZmianeRundy()
        {
            var brush = new SolidColorBrush(Colors.DarkGreen);
            EtykietaRundy.Foreground = brush;

            var animation = new ColorAnimation
            {
                From = Colors.DarkGreen,
                To = Colors.Red,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        // Sprawdza czy gracz zebrał wszystkie drzewa
        private void SprawdzCzyKoniecRundy()
        {
            if (iloscDrewna >= liczbaDrzewNaMapie)
            {
                timerEnergii.Stop();
                MessageBox.Show($"GRATULACJE, PRZEJDŹ DO NASTĘPNEJ RUNDY!\n(Aktualna runda: {liczbaRund})");
                liczbaRund++;
                EtykietaRundy.Content = $"Runda: {liczbaRund}";
                AnimujZmianeRundy();
                GenerujLosowaMape(20, 15); // Generuje nową mapę
            }
        }

        // Generuje losową mapę o podanych wymiarach
        private void GenerujLosowaMape(int szerokosc, int wysokosc)
        {
            Random rand = new Random();
            mapa = new int[wysokosc, szerokosc];
            szerokoscMapy = szerokosc;
            wysokoscMapy = wysokosc;
            liczbaDrzewNaMapie = 0;

            // Wypełnianie mapy losowymi terenami
            for (int y = 0; y < wysokosc; y++)
            {
                for (int x = 0; x < szerokosc; x++)
                {
                    int los = rand.Next(100); // Losowa wartość 0-99
                    if (los < 60) mapa[y, x] = LAKA;        // 60% szans na łąkę
                    else if (los < 85)                      // 25% szans na las
                    {
                        mapa[y, x] = LAS;
                        liczbaDrzewNaMapie++;
                    }
                    else if (los < 95) mapa[y, x] = SKALA;  // 10% szans na skały
                    else mapa[y, x] = JAGODY;               // 5% szans na jagody
                }
            }

            RysujMape(); // Rysuje mapę na ekranie
            iloscDrewna = 0;
            EtykietaDrewna.Content = $"Drewno: {iloscDrewna}/{liczbaDrzewNaMapie}";
            energia = 100;
            PasekEnergii.Value = 100;
            timerEnergii.Start(); // Restartuje timer energii
        }

        // Rysuje mapę w kontrolce SiatkaMapy
        private void RysujMape()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            // Tworzy wiersze i kolumny w siatce
            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            // Wypełnia siatkę obrazkami terenu
            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    Image obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]] // Ustawia odpowiednią teksturę
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            // Dodaje gracza na mapę
            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1); // Upewnia się że gracz jest na wierzchu
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();
        }

        // Uruchamia timer który co 2.5 sekundy zmniejsza energię
        private void StartTimerEnergii()
        {
            timerEnergii = new DispatcherTimer();
            timerEnergii.Interval = TimeSpan.FromSeconds(2.5);
            timerEnergii.Tick += (s, e) =>
            {
                energia -= 5; // Zmniejsza energię
                PasekEnergii.Value = energia;
                if (energia <= 0) // Sprawdza czy gracz ma jeszcze energię
                {
                    timerEnergii.Stop();
                    MessageBox.Show($"Koniec gry! Zabrakło energii. (Ukończone rundy: {liczbaRund - 1})");
                }
            };
            timerEnergii.Start();
        }

        // Ładuje tekstury terenu z plików
        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("Zdjecia/las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("Zdjecia/laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("Zdjecia/skala.png", UriKind.Relative));
            obrazyTerenu[JAGODY] = new BitmapImage(new Uri("Zdjecia/jagody.png", UriKind.Relative));
        }

        // Aktualizuje pozycję gracza na mapie
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        // Obsługa klawiatury
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            // Obsługa ruchu strzałkami
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;

            // Sprawdza czy nowa pozycja jest w granicach mapy
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                // Sprawdza czy gracz wszedł na skały
                if (mapa[nowyY, nowyX] == SKALA)
                {
                    WejdzNaSkale();
                }

                // Aktualizuje pozycję gracza
                pozycjaGraczaX = nowyX;
                pozycjaGraczaY = nowyY;
                AktualizujPozycjeGracza();
            }

            // Ścinanie drzew - klawisz E
            if (e.Key == Key.E && mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA; // Zamienia las na łąkę
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDrewna++;
                EtykietaDrewna.Content = $"Drewno: {iloscDrewna}/{liczbaDrzewNaMapie}";
                SprawdzCzyKoniecRundy(); // Sprawdza czy to koniec rundy
            }

            // Zbieranie jagód - klawisz E
            if (e.Key == Key.E && mapa[pozycjaGraczaY, pozycjaGraczaX] == JAGODY)
            {
                energia = Math.Min(100, energia + 20); // Dodaje energię (maks 100)
                PasekEnergii.Value = energia;
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA; // Zamienia jagody na łąkę
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
            }
        }

        // Obsługa wejścia na skały (kara energii)
        private void WejdzNaSkale()
        {
            int kara = energia / 2; // Zabiera połowę energii
            energia -= kara;
            PasekEnergii.Value = energia;

            if (energia <= 0) // Sprawdza czy gracz ma jeszcze energię
            {
                timerEnergii.Stop();
                MessageBox.Show($"Koniec gry! Wspinaczka wyczerpała Twoją energię. (Ukończone rundy: {liczbaRund - 1})");
            }
        }

        // Przycisk "Nowa Mapa" - resetuje grę
        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            liczbaRund = 1;
            EtykietaRundy.Content = $"Runda: {liczbaRund}";
            AnimujZmianeRundy();
            GenerujLosowaMape(20, 15);
        }

        // Obsługa przycisku zamknięcia
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Zamyka aplikację
        }
    }
} // Autor: K.S