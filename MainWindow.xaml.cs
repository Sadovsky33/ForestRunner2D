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
        public const int LAS = 1;     // las
        public const int LAKA = 2;     // łąka
        public const int SKALA = 3;    // skały
        public const int JAGODY = 4;   // jagody (jedzenie)
        public const int ILE_TERENOW = 5;

        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu;
        private const int RozmiarSegmentu = 32;
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        private Image obrazGracza;
        private int iloscDrewna = 0;
        private int energia = 100;
        private DispatcherTimer timerEnergii;
        private int liczbaRund = 1;
        private int liczbaDrzewNaMapie = 0;

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu,
                Source = new BitmapImage(new Uri("Zdjecia/gracz.png", UriKind.Relative))
            };

            StartTimerEnergii();
            GenerujLosowaMape(20, 15);
        }

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

        private void SprawdzCzyKoniecRundy()
        {
            if (iloscDrewna >= liczbaDrzewNaMapie)
            {
                timerEnergii.Stop();
                MessageBox.Show($"GRATULACJE, PRZEJDŹ DO NASTĘPNEJ RUNDY!\n(Aktualna runda: {liczbaRund})");
                liczbaRund++;
                EtykietaRundy.Content = $"Runda: {liczbaRund}";
                AnimujZmianeRundy();
                GenerujLosowaMape(20, 15);
            }
        }

        private void GenerujLosowaMape(int szerokosc, int wysokosc)
        {
            Random rand = new Random();
            mapa = new int[wysokosc, szerokosc];
            szerokoscMapy = szerokosc;
            wysokoscMapy = wysokosc;
            liczbaDrzewNaMapie = 0;

            for (int y = 0; y < wysokosc; y++)
            {
                for (int x = 0; x < szerokosc; x++)
                {
                    int los = rand.Next(100);
                    if (los < 60) mapa[y, x] = LAKA;
                    else if (los < 85)
                    {
                        mapa[y, x] = LAS;
                        liczbaDrzewNaMapie++;
                    }
                    else if (los < 95) mapa[y, x] = SKALA;
                    else mapa[y, x] = JAGODY;
                }
            }

            RysujMape();
            iloscDrewna = 0;
            EtykietaDrewna.Content = $"Drewno: {iloscDrewna}/{liczbaDrzewNaMapie}";
            energia = 100;
            PasekEnergii.Value = 100;
            timerEnergii.Start();
        }

        private void RysujMape()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    Image obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]]
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            SiatkaMapy.Children.Add(obrazGracza);
            Panel.SetZIndex(obrazGracza, 1);
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            AktualizujPozycjeGracza();
        }

        private void StartTimerEnergii()
        {
            timerEnergii = new DispatcherTimer();
            timerEnergii.Interval = TimeSpan.FromSeconds(2.5);
            timerEnergii.Tick += (s, e) =>
            {
                energia -= 5;
                PasekEnergii.Value = energia;
                if (energia <= 0)
                {
                    timerEnergii.Stop();
                    MessageBox.Show($"Koniec gry! Zabrakło energii. (Ukończone rundy: {liczbaRund - 1})");
                }
            };
            timerEnergii.Start();
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("Zdjecia/las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("Zdjecia/laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("Zdjecia/skala.png", UriKind.Relative));
            obrazyTerenu[JAGODY] = new BitmapImage(new Uri("Zdjecia/jagody.png", UriKind.Relative));
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;

            if (e.Key == Key.Up) nowyY--;
            else if (e.Key == Key.Down) nowyY++;
            else if (e.Key == Key.Left) nowyX--;
            else if (e.Key == Key.Right) nowyX++;

            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                if (mapa[nowyY, nowyX] == SKALA)
                {
                    WejdzNaSkale();
                }

                pozycjaGraczaX = nowyX;
                pozycjaGraczaY = nowyY;
                AktualizujPozycjeGracza();
            }

            // Ścinanie drzew - klawisz E
            if (e.Key == Key.E && mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
                iloscDrewna++;
                EtykietaDrewna.Content = $"Drewno: {iloscDrewna}/{liczbaDrzewNaMapie}";
                SprawdzCzyKoniecRundy();
            }

            // Zbieranie jagód - klawisz E
            if (e.Key == Key.E && mapa[pozycjaGraczaY, pozycjaGraczaX] == JAGODY)
            {
                energia = Math.Min(100, energia + 20);
                PasekEnergii.Value = energia;
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA;
                tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA];
            }
        }

        private void WejdzNaSkale()
        {
            int kara = energia / 2;
            energia -= kara;
            PasekEnergii.Value = energia;

            if (energia <= 0)
            {
                timerEnergii.Stop();
                MessageBox.Show($"Koniec gry! Wspinaczka wyczerpała Twoją energię. (Ukończone rundy: {liczbaRund - 1})");
            }
        }

        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            liczbaRund = 1;
            EtykietaRundy.Content = $"Runda: {liczbaRund}";
            AnimujZmianeRundy();
            GenerujLosowaMape(20, 15);
        }
    }
}

