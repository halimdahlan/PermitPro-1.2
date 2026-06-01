import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { ThreeJSOverlayView } from '@googlemaps/three';

let map;

const mapOptions = {
   tilt: 67,
   heading: 0,
   zoom: 18,
   center: {
      lat: 1.626067328980542,
      lng: 110.45197790308654,
   },
   mapId: '15431d2b469f209e',
   mapTypeId: 'satellite',
   // disable interactions due to animation loop and moveCamera
   disableDefaultUI: false,
   gestureHandling: 'none',
   keyboardShortcuts: false,
};

async function initMap() {
   const { Map } = await google.maps.importLibrary('maps');
   const mapDiv = document.getElementById('map');

   map = new Map(mapDiv, mapOptions);

   const scene = new THREE.Scene();
   const ambientLight = new THREE.AmbientLight(0xffffff, 0.75);

   scene.add(ambientLight);

   const directionalLight = new THREE.DirectionalLight(0xffffff, 0.25);

   directionalLight.position.set(0, 10, 50);
   scene.add(directionalLight);

   // Load the model.
   const loader = new GLTFLoader();
   const url = 'https://raw.githubusercontent.com/googlemaps/js-samples/main/assets/pin.gltf';

   //loader.load(url, (gltf) => {
   //   gltf.scene.scale.set(10, 10, 10);
   //   gltf.scene.rotation.x = Math.PI / 2;
   //   scene.add(gltf.scene);

   //   let { tilt, heading, zoom } = mapOptions;

   //   const animate = () => {
   //      if (tilt < 67.5) {
   //         tilt += 0.5;
   //      } else if (heading <= 360) {
   //         heading += 0.2;
   //         zoom -= 0.0005;
   //      } else {
   //         // exit animation loop
   //         return;
   //      }

   //      map.moveCamera({ tilt, heading, zoom });
   //      //requestAnimationFrame(animate);
   //   };

   //   //requestAnimationFrame(animate);
   //});

   new ThreeJSOverlayView({
      map,
      scene,
      anchor: { ...mapOptions.center, altitude: 100 },
      THREE,
   });
}

initMap();