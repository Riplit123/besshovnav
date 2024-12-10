using Microsoft.Maui.Layouts;
using System.Diagnostics;

namespace besshovnav
{
    public partial class MainPage : ContentPage
    {
        private bool isSelectingFrom = false;
        private bool isSelectingTo = false;

        private bool isTrackingLocation = false;

        private CancellationTokenSource cts;

        private (double latitude, double longitude)? fromLocation = null;
        private (double latitude, double longitude)? toLocation = null;
        private (double latitude, double longitude)? userLocation = null;

        private Point buildingUserPosition;

        private Dictionary<string, Rect> rooms = new Dictionary<string, Rect>();

        private Dictionary<int, List<View>> floorRooms = new Dictionary<int, List<View>>();

        private int currentFloor = 1;

        private double initialX, initialY;
        private bool isDraggingMarker = false;

        public MainPage()
        {
            InitializeComponent();

            var htmlSource = new HtmlWebViewSource
            {
                Html = GenerateHtml()
            };

            YandexMapView.Source = htmlSource;

            YandexMapView.Navigating += YandexMapView_Navigating;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnRoomTapped;

            InitializeRoomsAndFloors(tapGestureRecognizer);

            YandexMapView.Navigated += YandexMapView_Navigated;
        }

        private void OnIndoorUserMarkerPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (!isTrackingLocation)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:

                    isDraggingMarker = true;
                    break;

                case GestureStatus.Running:
                    if (isDraggingMarker)
                    {
                        double totalX = e.TotalX;
                        double totalY = e.TotalY;

                        double buildingWidth = BuildingView.Width;
                        double buildingHeight = BuildingView.Height;

                        double deltaX = totalX / buildingWidth;
                        double deltaY = totalY / buildingHeight;

                        double newX = buildingUserPosition.X + deltaX;
                        double newY = buildingUserPosition.Y + deltaY;

                        newX = Math.Max(0, Math.Min(1, newX));
                        newY = Math.Max(0, Math.Min(1, newY));

                        buildingUserPosition = new Point(newX, newY);

                        UpdateIndoorUserMarkerPosition();
                        CheckUserInRoom();
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:

                    isDraggingMarker = false;
                    break;
            }
        }

        private void InitializeRoomsAndFloors(TapGestureRecognizer tapGestureRecognizer)
        {
          
            floorRooms.Clear();

            // Этаж 1
            floorRooms[1] = new List<View> { Room1_2, floorone };
           // Room1_1.GestureRecognizers.Add(tapGestureRecognizer);
            Room1_2.GestureRecognizers.Add(tapGestureRecognizer);
            floorone.GestureRecognizers.Add(tapGestureRecognizer);


            // Этаж 2
            floorRooms[2] = new List<View> { Room2_1, floortwo };
            Room2_1.GestureRecognizers.Add(tapGestureRecognizer);
            floortwo.GestureRecognizers.Add(tapGestureRecognizer);

            // Этаж 3
            floorRooms[3] = new List<View> { Room3_1, floorthree };
            Room3_1.GestureRecognizers.Add(tapGestureRecognizer);
         //   Room3_2.GestureRecognizers.Add(tapGestureRecognizer);
         //   Room3_3.GestureRecognizers.Add(tapGestureRecognizer);
         //   Room3_4.GestureRecognizers.Add(tapGestureRecognizer);
            floorthree.GestureRecognizers.Add(tapGestureRecognizer);

            // Этаж 4
            floorRooms[4] = new List<View> { Room4_1, Room4_2, Room4_3, Room4_4 };
            Room4_1.GestureRecognizers.Add(tapGestureRecognizer);
            Room4_2.GestureRecognizers.Add(tapGestureRecognizer);
            Room4_3.GestureRecognizers.Add(tapGestureRecognizer);
            Room4_4.GestureRecognizers.Add(tapGestureRecognizer);

            // Этаж 5
            floorRooms[5] = new List<View> { Room5_1 };
            Room5_1.GestureRecognizers.Add(tapGestureRecognizer);

          
            //Room1_1.StyleId = "Room1_1";
            Room1_2.StyleId = "холл";
            Room2_1.StyleId = "зал";
            Room3_1.StyleId = "IT-квантум";
          //  Room3_2.StyleId = "Room3_2";
          //  Room3_3.StyleId = "Room3_3";
          //  Room3_4.StyleId = "Room3_4";
            Room4_1.StyleId = "Room4_1";
            Room4_2.StyleId = "Room4_2";
            Room4_3.StyleId = "Room4_3";
            Room4_4.StyleId = "Room4_4";
            Room5_1.StyleId = "Room5_1";

            ShowRoomsForFloor(currentFloor);
        }

        private void ShowRoomsForFloor(int floor)
        {
            
            foreach (var floorList in floorRooms.Values)
            {
                foreach (var room in floorList)
                {
                    room.IsVisible = false;
                }
            }

            
            if (floorRooms.ContainsKey(floor))
            {
                foreach (var room in floorRooms[floor])
                {
                    room.IsVisible = true;
                }
            }

            
            FloorLabel.Text = $"Этаж {floor}";
        }




      
        private string GenerateHtml()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
            <script src='https://api-maps.yandex.ru/2.1/?apikey=9f4d093c-51a6-4adf-aa7b-1a48afc68077&lang=ru_RU' type='text/javascript'></script>
            </head>
            <body>
             <!-- Контейнер для карты -->
                 <div id='map' style='width: 100%; height: 100vh; padding: 0; margin: 0;'></div>

                <script type='text/javascript'>
                var map;
                var fromPlacemark, toPlacemark, route;
                var userPlacemark;
                var buildingOverlay;
                var currentZoom;

                ymaps.ready(init);

                function init() {
                    map = new ymaps.Map('map', {
                        center: [56.326863, 44.009911],
                        zoom: 16,
                        controls: [],
                        minZoom: 10,
                       maxZoom: 18
                    });

            // Обработка кликов по карте
            map.events.add('click', function (e) {
                var coords = e.get('coords');

                // Передача данных в C# код
                window.location = 'callback://mapClick/' + coords.join(',');
            });

            // Обработка события изменения области карты
             map.events.add('boundschange', function (e) {
             var newZoom = e.get('newZoom');
             var minZoom = map.options.get('minZoom');
             var maxZoom = map.options.get('maxZoom');

             if (newZoom < minZoom) {
            map.setZoom(minZoom);
                } else if (newZoom > maxZoom) {
            map.setZoom(maxZoom);
            } else {
            var zoom = map.getZoom();
            var center = map.getCenter();
            // Передаем уровень зума и центр карты в C# код
            window.location = 'callback://viewChanged/' + zoom + '/' + center[0] + '/' + center[1];
             }
             });
            // Добавление меток или других элементов на карту, если необходимо
        }

        function setPoint(type, lat, lon) {
            var coords = [parseFloat(lat), parseFloat(lon)];
            if (type === 'from') {
                if (fromPlacemark) {
                    fromPlacemark.geometry.setCoordinates(coords);
                } else {
                    fromPlacemark = new ymaps.Placemark(coords, { iconContent: 'A' }, {
                        preset: 'islands#greenDotIconWithCaption'
                    });
                    map.geoObjects.add(fromPlacemark);
                }
            } else if (type === 'to') {
                if (toPlacemark) {
                    toPlacemark.geometry.setCoordinates(coords);
                } else {
                    toPlacemark = new ymaps.Placemark(coords, { iconContent: 'B' }, {
                        preset: 'islands#redDotIconWithCaption'
                    });
                    map.geoObjects.add(toPlacemark);
                }
            }
            buildRoute();
        }

        function buildRoute() {
            if (fromPlacemark && toPlacemark) {
                if (route) {
                    map.geoObjects.remove(route);
                }
                ymaps.route([fromPlacemark.geometry.getCoordinates(), toPlacemark.geometry.getCoordinates()], {
                    routingMode: 'pedestrian' // Пеший маршрут
                }).then(function (router) {
                    route = router;
                    map.geoObjects.add(route);
                    var distance = router.getHumanLength();
                    var time = router.getHumanTime();
                    window.location = 'callback://routeInfo/' + encodeURIComponent(distance + ',' + time);
                }, function (error) {
                    // Обработка ошибки построения маршрута
                    window.location = 'callback://routeError/' + encodeURIComponent(error.message);
                });
            }
        }

        function clearRoute() {
            if (route) {
                map.geoObjects.remove(route);
                route = null;
            }
            if (fromPlacemark) {
                map.geoObjects.remove(fromPlacemark);
                fromPlacemark = null;
            }
            if (toPlacemark) {
                map.geoObjects.remove(toPlacemark);
                toPlacemark = null;
            }
        }

        function setUserLocation(lat, lon) {
            var coords = [parseFloat(lat), parseFloat(lon)];
            if (userPlacemark) {
                userPlacemark.geometry.setCoordinates(coords);
            } else {
                userPlacemark = new ymaps.Placemark(coords, {}, {
                    preset: 'islands#circleIcon',
                    iconColor: '#1E98FF'
                });
                map.geoObjects.add(userPlacemark);
            }
        }

        function removeUserLocation() {
            if (userPlacemark) {
                map.geoObjects.remove(userPlacemark);
                userPlacemark = null;
            }
        }
    </script>
</body>
</html>";
        }

       
        private void YandexMapView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            var url = e.Url;

            if (url.StartsWith("callback://"))
            {
                e.Cancel = true; 

                var data = Uri.UnescapeDataString(url.Substring("callback://".Length));

                if (data.StartsWith("mapClick/"))
                {
                    
                    var coordsData = data.Substring("mapClick/".Length).Split(',');
                    if (coordsData.Length == 2)
                    {
                        double latitude = Convert.ToDouble(coordsData[0], System.Globalization.CultureInfo.InvariantCulture);
                        double longitude = Convert.ToDouble(coordsData[1], System.Globalization.CultureInfo.InvariantCulture);

                        if (isSelectingFrom)
                        {
                           
                            fromLocation = (latitude, longitude);
                            YandexMapView.EvaluateJavaScriptAsync($"setPoint('from', {latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                        }
                        else if (isSelectingTo)
                        {
                            
                            toLocation = (latitude, longitude);
                            YandexMapView.EvaluateJavaScriptAsync($"setPoint('to', {latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");

                            
                            if (fromLocation == null && userLocation != null)
                            {
                                fromLocation = userLocation;
                                YandexMapView.EvaluateJavaScriptAsync($"setPoint('from', {userLocation.Value.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {userLocation.Value.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                            }
                        }

                       
                        isSelectingFrom = false;
                        isSelectingTo = false;
                    }
                }
                else if (data.StartsWith("routeInfo/"))
                {
                 
                    var infoData = data.Substring("routeInfo/".Length).Split(',');
                    if (infoData.Length == 2)
                    {
                        string distance = infoData[0];
                        string time = infoData[1];

                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await DisplayAlert("Маршрут построен", $"Длина: {distance}, Время: {time}", "OK");
                        });
                        ToButton.BackgroundColor = Color.FromHex("#2ec095");
                    }
                }
                else if (data.StartsWith("routeError/"))
                {
                    
                    var errorMessage = data.Substring("routeError/".Length);

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Ошибка при построении маршрута", errorMessage, "OK");
                    });
                }
                else if (data.StartsWith("viewChanged/"))
                {
            

                    var parts = data.Substring("viewChanged/".Length).Split('/');

                    if (parts.Length == 3 &&
                        double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double zoomLevel) &&
                        double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double centerLat) &&
                        double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double centerLon))
                    {
                  
                        double buildingLat = 56.326863;
                        double buildingLon = 44.009911;
                        double radiusInMeters = 100; 
                      
                        double distance = GetDistance(centerLat, centerLon, buildingLat, buildingLon);

                    
                        bool shouldShowBuildingView = (zoomLevel >= 18) && (distance <= radiusInMeters);

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            BuildingView.IsVisible = shouldShowBuildingView;

           
                            ControlPanel.IsVisible = shouldShowBuildingView;
                        });
                    }
                }
            }
        }




        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371e3; 
            double φ1 = lat1 * Math.PI / 180;
            double φ2 = lat2 * Math.PI / 180;
            double Δφ = (lat2 - lat1) * Math.PI / 180;
            double Δλ = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                       Math.Cos(φ1) * Math.Cos(φ2) *
                       Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = R * c; 

            return distance;
        }

     
        private void YandexMapView_Navigated(object sender, WebNavigatedEventArgs e)
        {
           
        }

   
        private void OnFromButtonClicked(object sender, EventArgs e)
        {
            isSelectingFrom = true;
            isSelectingTo = false;
            FromButton.BackgroundColor = Color.FromHex("#03624c");
            ToButton.BackgroundColor = Color.FromHex("#2ec095");
        }

       
        private void OnToButtonClicked(object sender, EventArgs e)
        {
            isSelectingFrom = false;
            isSelectingTo = true;
            FromButton.BackgroundColor = Color.FromHex("#2ec095");
            ToButton.BackgroundColor = Color.FromHex("#03624c");
        }

       
        private void OnClearRouteButtonClicked(object sender, EventArgs e)
        {
            fromLocation = null;
            toLocation = null;
            YandexMapView.EvaluateJavaScriptAsync("clearRoute();");
        }

      
        private void OnFloorButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (int.TryParse(button.CommandParameter.ToString(), out int floor))
            {
                currentFloor = floor;
                ShowRoomsForFloor(currentFloor);
            }
        }

      
        private async void OnLocationButtonClicked(object sender, EventArgs e)
        {
            isTrackingLocation = !isTrackingLocation;

            if (isTrackingLocation)
            {
                LocationButton.BackgroundColor = Color.FromHex("#03624c");

                cts = new CancellationTokenSource();

                try
                {
                   
                    var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Доступ запрещен", "Необходимо разрешить доступ к местоположению.", "OK");
                        isTrackingLocation = false;
                        LocationButton.BackgroundColor = Color.FromHex("#03624c");
                        return;
                    }

                  
                    await UpdateUserLocation();

                  
                    if (!Accelerometer.IsMonitoring)
                        Accelerometer.Start(SensorSpeed.UI);

                    if (!Compass.IsMonitoring)
                        Compass.Start(SensorSpeed.UI);

                    Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
                    Compass.ReadingChanged += Compass_ReadingChanged;

                
                    buildingUserPosition = new Point(0.5, 0.9); 

                
                    IndoorUserMarker.IsVisible = true;
                    UpdateIndoorUserMarkerPosition();

                    
                    Device.StartTimer(TimeSpan.FromSeconds(5), () =>
                    {
                        if (isTrackingLocation)
                        {
                            UpdateUserLocation();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
            else
            {
                LocationButton.BackgroundColor = Color.FromHex("#2ec095");

                if (cts != null)
                {
                    cts.Cancel();
                    cts = null;
                }

                userLocation = null;

                await YandexMapView.EvaluateJavaScriptAsync("removeUserLocation();");

  
                if (Accelerometer.IsMonitoring)
                    Accelerometer.Stop();

                if (Compass.IsMonitoring)
                    Compass.Stop();

                Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
                Compass.ReadingChanged -= Compass_ReadingChanged;

     
                IndoorUserMarker.IsVisible = false;
            }
        }


        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
          
            double acceleration = Math.Sqrt(e.Reading.Acceleration.X * e.Reading.Acceleration.X +
                                            e.Reading.Acceleration.Y * e.Reading.Acceleration.Y +
                                            e.Reading.Acceleration.Z * e.Reading.Acceleration.Z);

            if (acceleration > 1.2)
            {
          
                UpdateIndoorUserPosition();
            }
        }

   
        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
        
            userDirection = e.Reading.HeadingMagneticNorth;
        }

        private double userDirection = 0;

        private void UpdateIndoorUserPosition()
        {
            double stepLength = 0.01;

         
            double directionRadians = (Math.PI / 180) * userDirection;

       
            double deltaX = stepLength * Math.Sin(directionRadians);
            double deltaY = -stepLength * Math.Cos(directionRadians);

            double newX = buildingUserPosition.X + deltaX;
            double newY = buildingUserPosition.Y + deltaY;

            if (newX >= 0 && newX <= 1 && newY >= 0 && newY <= 1)
            {
                buildingUserPosition = new Point(newX, newY);
                UpdateIndoorUserMarkerPosition();

                CheckUserInRoom();
            }
        }

        private void UpdateIndoorUserMarkerPosition()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                AbsoluteLayout.SetLayoutBounds(IndoorUserMarker, new Rect(buildingUserPosition.X, buildingUserPosition.Y, 0.02, 0.02));
                AbsoluteLayout.SetLayoutFlags(IndoorUserMarker, AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
            });
        }

        private void CheckUserInRoom()
        {
         
            if (floorRooms.ContainsKey(currentFloor))
            {
                foreach (var room in floorRooms[currentFloor])
                {
                    if (room is BoxView boxView)
                    {
                        var bounds = AbsoluteLayout.GetLayoutBounds(boxView);
                        var flags = AbsoluteLayout.GetLayoutFlags(boxView);

                        double roomX = bounds.X;
                        double roomY = bounds.Y;
                        double roomWidth = bounds.Width;
                        double roomHeight = bounds.Height;
                        if (flags.HasFlag(AbsoluteLayoutFlags.All))
                        {
                         
                            if (buildingUserPosition.X >= roomX && buildingUserPosition.X <= roomX + roomWidth &&
                                buildingUserPosition.Y >= roomY && buildingUserPosition.Y <= roomY + roomHeight)
                            {
                        
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    FloorLabel.Text = $"Этаж {currentFloor}, {room.StyleId}";
                                });
                                return;
                            }
                        }
                    }
                }
            }
          
            Device.BeginInvokeOnMainThread(() =>
            {
                FloorLabel.Text = $"Этаж {currentFloor}";
            });
        }

        private bool IsUserInBuilding(double latitude, double longitude)
        {
        
            double buildingLat = 56.326863;
            double buildingLon = 44.009911;
            double radiusInMeters = 50; 
            double distance = GetDistance(latitude, longitude, buildingLat, buildingLon);

            return distance <= radiusInMeters;
        }

        private async Task UpdateUserLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                var location = await Geolocation.GetLocationAsync(request, cts?.Token ?? CancellationToken.None);

                if (location != null)
                {
                    userLocation = (location.Latitude, location.Longitude);

                    await YandexMapView.EvaluateJavaScriptAsync($"setUserLocation({location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении местоположения: {ex.Message}");
            }
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            routeone.IsVisible = true;
            var button = sender as Button;
            if (int.TryParse(button.CommandParameter.ToString(), out int floor))
            {
                currentFloor = floor;
                ShowRoomsForFloor(currentFloor);
            }
        }

     
        private void OnRoomTapped(object sender, EventArgs e)
        {
            if (isSelectingTo)
            {
                var room = sender as View;

                var roomId = room.StyleId;

                var buildingEntranceCoords = (latitude: 56.326863, longitude: 44.009911);

                YandexMapView.EvaluateJavaScriptAsync($"setPoint('to', {buildingEntranceCoords.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {buildingEntranceCoords.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");

                routetwo.IsVisible = true;

                if (fromLocation == null && userLocation != null)
                {
                    fromLocation = userLocation;
                    YandexMapView.EvaluateJavaScriptAsync($"setPoint('from', {userLocation.Value.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {userLocation.Value.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                }

                isSelectingTo = false;
            }
        }
    }
}




















           

