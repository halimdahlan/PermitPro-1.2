const mapStyle = {
   retro: [
      { elementType: 'geometry', stylers: [{ color: '#ebe3cd' }] },
      { elementType: 'labels.text.fill', stylers: [{ color: '#523735' }] },
      { elementType: 'labels.text.stroke', stylers: [{ color: '#f5f1e6' }] },
      {
         featureType: 'administrative',
         elementType: 'geometry.stroke',
         stylers: [{ color: '#c9b2a6' }],
      },
      {
         featureType: 'administrative.land_parcel',
         elementType: 'geometry.stroke',
         stylers: [{ color: '#dcd2be' }],
      },
      {
         featureType: 'administrative.land_parcel',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#ae9e90' }],
      },
      {
         featureType: 'landscape.natural',
         elementType: 'geometry',
         stylers: [{ color: '#dfd2ae' }],
      },
      {
         featureType: 'poi',
         elementType: 'geometry',
         stylers: [{ color: '#dfd2ae' }],
      },
      {
         featureType: 'poi',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#93817c' }],
      },
      {
         featureType: 'poi.park',
         elementType: 'geometry.fill',
         stylers: [{ color: '#a5b076' }],
      },
      {
         featureType: 'poi.park',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#447530' }],
      },
      {
         featureType: 'road',
         elementType: 'geometry',
         stylers: [{ color: '#f5f1e6' }],
      },
      {
         featureType: 'road.arterial',
         elementType: 'geometry',
         stylers: [{ color: '#fdfcf8' }],
      },
      {
         featureType: 'road.highway',
         elementType: 'geometry',
         stylers: [{ color: '#f8c967' }],
      },
      {
         featureType: 'road.highway',
         elementType: 'geometry.stroke',
         stylers: [{ color: '#e9bc62' }],
      },
      {
         featureType: 'road.highway.controlled_access',
         elementType: 'geometry',
         stylers: [{ color: '#e98d58' }],
      },
      {
         featureType: 'road.highway.controlled_access',
         elementType: 'geometry.stroke',
         stylers: [{ color: '#db8555' }],
      },
      {
         featureType: 'road.local',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#806b63' }],
      },
      {
         featureType: 'transit.line',
         elementType: 'geometry',
         stylers: [{ color: '#dfd2ae' }],
      },
      {
         featureType: 'transit.line',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#8f7d77' }],
      },
      {
         featureType: 'transit.line',
         elementType: 'labels.text.stroke',
         stylers: [{ color: '#ebe3cd' }],
      },
      {
         featureType: 'transit.station',
         elementType: 'geometry',
         stylers: [{ color: '#dfd2ae' }],
      },
      {
         featureType: 'water',
         elementType: 'geometry.fill',
         stylers: [{ color: '#b9d3c2' }],
      },
      {
         featureType: 'water',
         elementType: 'labels.text.fill',
         stylers: [{ color: '#92998d' }],
      },
   ]
};



var toggleHighlight = (markerView, property, map) => {
   if (markerView.content.classList.contains('highlight')) {
      markerView.content.classList.remove('highlight');
      markerView.zIndex = null;
   } else {
      markerView.content.classList.add('highlight');
      markerView.zIndex = 1;
   }

   map.panTo({
      lat: property.position.lat,
      lng: property.position.lng
   });
};


var customPin = (property) => {
   const customPin = document.createElement('div');
   const customPin2 = document.createElement('div');

   customPin2.classList.add('site-pin');
   customPin2.innerHTML = `
   <div>
      <i class="fa-duotone fa-user-helmet-safety fa-xl fa-fade"></i>
   </div>
   `;

   customPin.classList.add('site-pin');
   customPin.innerHTML = `
   <div>
      <div class="d-flex flex-row mb-1">
         <div class="pin-active"><i class="fa-sharp fa-solid fa-circle"></i></div>
         <div class="ms-2 pin-active pin-text">ACTIVE</div>
         <div class="pin-permit-count">3</div>
      </div>
      <div class="d-flex flex-row mb-1">
         <div class="pin-approved"><i class="fa-sharp fa-solid fa-circle"></i></div>
         <div class="ms-2 pin-approved pin-text">APPROVED</div>
         <div class="pin-permit-count">5</div>
      </div>
      <div class="d-flex flex-row">
         <div class="pin-rejected"><i class="fa-sharp fa-solid fa-circle"></i></div>
         <div class="ms-2 pin-rejected pin-text">REJECTED</div>
         <div class="pin-permit-count">2</div>
      </div>
   </div>
   `;

   return customPin2;
};


var buildContent = (property) => {
   const content = document.createElement('div');

   content.classList.add('property');
   content.innerHTML = `
      <div class='icon'>
         <i aria-hidden='true' class='fa fa-icon fa-${property.type}' title='${property.type}'></i>
         <span class='fa-sr-only'>${property.type}</span>
      </div>
      <div class='details'>
         <div class='price'>${property.price}</div>
         <div class='address'>${property.address}</div>
         <div class='features'>
         <div>
            <i aria-hidden='true' class='fa fa-bed fa-lg bed' title='bedroom'></i>
            <span class='fa-sr-only'>bedroom</span>
            <span>${property.bed}</span>
         </div>
         <div>
            <i aria-hidden='true' class='fa fa-bath fa-lg bath' title='bathroom'></i>
            <span class='fa-sr-only'>bathroom</span>
            <span>${property.bath}</span>
         </div>
         <div>
            <i aria-hidden='true' class='fa fa-ruler fa-lg size' title='size'></i>
            <span class='fa-sr-only'>size</span>
            <span>${property.size} ft<sup>2</sup></span>
         </div>
         </div>
      </div>`;

   return content;
};


const properties = [
   {
      address: 'PME & Bitumen Facility',
      description: 'PME & Bitumen Facility',
      price: '$ 3,889,000',
      type: 'home',
      bed: 5,
      bath: 4.5,
      size: 300,
      position: {
         lat: 1.626067328980542,  
         lng: 110.45197790308654,
      },
   },
   {
      address: 'Senari Synergy',
      description: 'Oil Terminal',
      price: '$ 3,889,000',
      type: 'office',
      bed: 5,
      bath: 4.5,
      size: 300,
      position: {
         lat: 1.6247300114492098, 
         lng: 110.4492453828483,
      },
   },
   {
      address: 'IOT Senari LPG',
      description: 'IOT Senari LPG',
      price: '$ 3,889,000',
      type: 'office',
      bed: 5,
      bath: 4.5,
      size: 300,
      position: {
         lat: 1.6283012742962009, 
         lng: 110.4548994791698,
      },
   },
];

var url = '/' + $('#hidCompanyId').val() + '/sites/map/location/0';

var ajaxGet = $.ajax({
   url: url,
   type: 'GET',
   dataType: 'json',
   success: function (data) {
      console.log(data);
   },
   error: function (error) {
      console.log(error);
   }
});

ajaxGet.done(function (data) {
   console.log(data);
});

//initMap();


function getPermitsByLocation() {
}


/**
 * Function to initialize the map
 */
async function initMap() {
   const { Map, InfoWindow } = await google.maps.importLibrary('maps');
   const { AdvancedMarkerElement } = await google.maps.importLibrary('marker');
   const center = {
      lat: 1.626067328980542,
      lng: 110.45197790308654,
   };

   const map = new Map(document.getElementById('map'), {
      disableDefaultUI: true,
      zoom: 17,
      center,
      mapId: 'dd60996d28e89f78',
      mapTypeId: google.maps.MapTypeId.SATELLITE,
      mapTypeControl: false,
      tilt: 0,
   });

   const infoWindow = new InfoWindow();

   for (const property of properties) {
      const advancedMarkerElement = new AdvancedMarkerElement({
         map,
         content: customPin(property), //buildContent(property),
         position: property.position,
         title: property.description,
      });

      advancedMarkerElement.addListener('click', () => {
         //toggleHighlight(AdvancedMarkerElement, property, map);

         const content = `
         <div>
            <h4>${property.address}</h4>
            <div class="mb-3">${property.description}</div>
            <div>
               <div class="d-flex flex-row mb-1">
                  <div class="pin-active"><i class="fa-sharp fa-solid fa-circle"></i></div>
                  <div class="ms-2 pin-active pin-text">ACTIVE</div>
                  <div class="pin-permit-count">3</div>
               </div>
               <div class="d-flex flex-row mb-1">
                  <div class="pin-approved"><i class="fa-sharp fa-solid fa-circle"></i></div>
                  <div class="ms-2 pin-approved pin-text">APPROVED</div>
                  <div class="pin-permit-count">5</div>
               </div>
               <div class="d-flex flex-row">
                  <div class="pin-rejected"><i class="fa-sharp fa-solid fa-circle"></i></div>
                  <div class="ms-2 pin-rejected pin-text">REJECTED</div>
                  <div class="pin-permit-count">2</div>
               </div>
            </div>
         </div>
         `;

         infoWindow.close();

         infoWindow.setOptions({
            minWidth: 300,
         });

         infoWindow.setContent(content);

         infoWindow.open({
            anchor: advancedMarkerElement,
            map,
         });

         map.panTo(property.position);

      });
   }
}
