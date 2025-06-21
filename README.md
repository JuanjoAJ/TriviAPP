# 🎮 Multiplayer Trivia Game – Unity + Photon + Firebase

This is a multiplayer trivia game inspired by Kahoot, built using Unity and Photon for real-time multiplayer gameplay.  
It uses Firebase for authentication and stores all questions in Firebase Realtime Database, which are loaded from a custom backend API.

## 💡 Features

- 👥 Multiplayer rooms with up to 4 players using Photon (PUN 2)
- ❓ Trivia questions loaded from Firebase (fed by custom .NET API)
- 🔐 User authentication with Firebase Authentication
- 🏆 Real-time gameplay: synchronized questions, answer lock-in, score tracking
- 🔄 Server-synced question rotation and player status
- 📊 Game results at the end

## 🛠️ Technologies Used

- Unity
- Photon PUN 2
- Firebase Realtime Database
- Firebase Authentication
- Custom .NET backend (for managing/tracking questions and syncing with Firebase)
- REST APIs (to feed Firebase from OpenTDB and other trivia sources)

## 🔐 Authentication

- Firebase Authentication is used for user login.
- Supports email/password or anonymous login.
- Player data is linked to their Firebase UID.

# 🎮 Juego de Trivia Multijugador – Unity + Photon + Firebase

Este es un juego de trivia multijugador inspirado en Kahoot, desarrollado con Unity y Photon para una experiencia en tiempo real.  
Utiliza Firebase para la autenticación de usuarios y para almacenar las preguntas, las cuales son cargadas desde una API propia creada en .NET.

## 💡 Funcionalidades

- 👥 Salas multijugador de hasta 4 jugadores usando Photon (PUN 2)
- ❓ Preguntas de trivia almacenadas en Firebase y alimentadas desde una API propia
- 🔐 Autenticación de usuarios con Firebase Authentication
- 🏆 Sincronización de preguntas, bloqueo de respuestas, conteo de puntos en tiempo real
- 🔄 Rotación de preguntas y estados sincronizados entre jugadores
- 📊 Resultados del juego al finalizar la partida

## 🛠️ Tecnologías Utilizadas

- Unity
- Photon (PUN 2)
- Firebase Realtime Database
- Firebase Authentication
- API propia en .NET (para cargar y gestionar preguntas)
- REST API (para obtener preguntas desde OpenTDB y otras APIs externas)

## 🔐 Autenticación

- Se utiliza Firebase Authentication.
- Soporta inicio de sesión con correo/contraseña o anónimo.
- Los datos del jugador se vinculan al UID de Firebase.
