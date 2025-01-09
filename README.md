# Bike Simulator real motorcycle dynamics
 A bike simulator with real world engine. This is a bike simulator for a Royal Enfield Classic 350. The simulator emulates the real world engine performance with real world data (Torque, Engine Power, RPM)
 ![Bike_input](https://github.com/user-attachments/assets/ec965cf5-9add-4b41-86b6-0430d1053c9c)

The Real World Data for Torque Vs RPM Curve has been derived from a dynamometer test of Classic 350. The Graph can be edited for any motorcycle according to it's engine Capabilities and Performance. The CodeBase enhances the bike perfromance at various gear ratios as it would in real life, the gear ratio of any bike can be found in their specification page.
![2016012195152440](https://github.com/user-attachments/assets/15a3bcfa-a2cb-4f89-9e07-1694ecc127c9)

The Simulator does not take into the account drag force, centripetal force, lateral and horizontal dynamics as it would in real life, but it tries to simulate something like that with constraints. For example, the lean vs Steer angle has been constrained at various speed, instead of calculating the offset fork, steer torque force and turn radius. Instead, it simulated the lean and steer by off balancing at z-axis by the lean VS Steer graph according to bike's velocity.
![bike_lean](https://github.com/user-attachments/assets/7f0b13c9-c59b-4774-a4a1-36c971de83db)

CAUTION :- This is still in Developement Stages and is not Production Ready. This Project will be updated with more real world dyanmics (Center of Mass, Torque Forces Calculated at every Degree of Freedom, And Many more such things)
