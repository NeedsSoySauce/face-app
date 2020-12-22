using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace face_project
{


    public struct FaceDetectionResult
    {
        public string Response;
        public IList<DetectedFace> Faces;
    }

    class FaceService
    {
        static string personGroupId = Guid.NewGuid().ToString();

        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        public static async Task<FaceDetectionResult> DetectFacesFromStream(IFaceClient client, Stream image, string recognitionModel = RecognitionModel.Recognition03)
        {
            StringBuilder builder = new StringBuilder();

            IList<DetectedFace> detectedFaces;

            // Detect faces with all attributes from image
            detectedFaces = await client.Face.DetectWithStreamAsync(image,
                    returnFaceAttributes: new List<FaceAttributeType?> { FaceAttributeType.Accessories, FaceAttributeType.Age,
                        FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair,
                        FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, FaceAttributeType.HeadPose,
                        FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile },
                    // We specify detection model 1 because we are retrieving attributes.
                    detectionModel: DetectionModel.Detection01,
                    recognitionModel: recognitionModel);

            builder.AppendLine($"{detectedFaces.Count} face(s) detected from image.");

            var faceRectangles = new List<FaceRectangle>();

            foreach (var face in detectedFaces)
            {
                builder.AppendLine($"Attributes for face id {face.FaceId}:");

                // Get bounding box of the faces
                builder.AppendLine($"Rectangle(Left/Top/Width/Height) : {face.FaceRectangle.Left} {face.FaceRectangle.Top} {face.FaceRectangle.Width} {face.FaceRectangle.Height}");
                faceRectangles.Add(face.FaceRectangle);

                // Get accessories of the faces
                List<Accessory> accessoriesList = (List<Accessory>)face.FaceAttributes.Accessories;
                int count = face.FaceAttributes.Accessories.Count;
                string accessory; string[] accessoryArray = new string[count];
                if (count == 0) { accessory = "NoAccessories"; }
                else
                {
                    for (int i = 0; i < count; ++i) { accessoryArray[i] = accessoriesList[i].Type.ToString(); }
                    accessory = string.Join(",", accessoryArray);
                }
                builder.AppendLine($"Accessories : {accessory}");

                // Get face other attributes
                builder.AppendLine($"Age : {face.FaceAttributes.Age}");
                builder.AppendLine($"Blur : {face.FaceAttributes.Blur.BlurLevel}");

                // Get emotion on the face
                string emotionType = string.Empty;
                double emotionValue = 0.0;
                Emotion emotion = face.FaceAttributes.Emotion;
                if (emotion.Anger > emotionValue) { emotionValue = emotion.Anger; emotionType = "Anger"; }
                if (emotion.Contempt > emotionValue) { emotionValue = emotion.Contempt; emotionType = "Contempt"; }
                if (emotion.Disgust > emotionValue) { emotionValue = emotion.Disgust; emotionType = "Disgust"; }
                if (emotion.Fear > emotionValue) { emotionValue = emotion.Fear; emotionType = "Fear"; }
                if (emotion.Happiness > emotionValue) { emotionValue = emotion.Happiness; emotionType = "Happiness"; }
                if (emotion.Neutral > emotionValue) { emotionValue = emotion.Neutral; emotionType = "Neutral"; }
                if (emotion.Sadness > emotionValue) { emotionValue = emotion.Sadness; emotionType = "Sadness"; }
                if (emotion.Surprise > emotionValue) { emotionType = "Surprise"; }
                builder.AppendLine($"Emotion : {emotionType}");

                // Get more face attributes
                builder.AppendLine($"Exposure : {face.FaceAttributes.Exposure.ExposureLevel}");
                builder.AppendLine($"FacialHair : {string.Format("{0}", face.FaceAttributes.FacialHair.Moustache + face.FaceAttributes.FacialHair.Beard + face.FaceAttributes.FacialHair.Sideburns > 0 ? "Yes" : "No")}");
                builder.AppendLine($"Gender : {face.FaceAttributes.Gender}");
                builder.AppendLine($"Glasses : {face.FaceAttributes.Glasses}");

                // Get hair color
                Hair hair = face.FaceAttributes.Hair;
                string color = null;
                if (hair.HairColor.Count == 0) { if (hair.Invisible) { color = "Invisible"; } else { color = "Bald"; } }
                HairColorType returnColor = HairColorType.Unknown;
                double maxConfidence = 0.0f;
                foreach (HairColor hairColor in hair.HairColor)
                {
                    if (hairColor.Confidence <= maxConfidence) { continue; }
                    maxConfidence = hairColor.Confidence; returnColor = hairColor.Color; color = returnColor.ToString();
                }
                builder.AppendLine($"Hair : {color}");

                // Get more attributes
                builder.AppendLine($"HeadPose : {string.Format("Pitch: {0}, Roll: {1}, Yaw: {2}", Math.Round(face.FaceAttributes.HeadPose.Pitch, 2), Math.Round(face.FaceAttributes.HeadPose.Roll, 2), Math.Round(face.FaceAttributes.HeadPose.Yaw, 2))}");
                builder.AppendLine($"Makeup : {string.Format("{0}", (face.FaceAttributes.Makeup.EyeMakeup || face.FaceAttributes.Makeup.LipMakeup) ? "Yes" : "No")}");
                builder.AppendLine($"Noise : {face.FaceAttributes.Noise.NoiseLevel}");
                builder.AppendLine($"EyeOccluded: {face.FaceAttributes.Occlusion.EyeOccluded}");
                builder.AppendLine($"ForeheadOccluded: {face.FaceAttributes.Occlusion.ForeheadOccluded}");
                builder.AppendLine($"MouthOccluded: {face.FaceAttributes.Occlusion.MouthOccluded}");
                builder.AppendLine($"Smile : {face.FaceAttributes.Smile}");
                builder.AppendLine();
            }

            // return builder.ToString();
            return new FaceDetectionResult
            {
                Response = builder.ToString(),
                Faces = detectedFaces
            };
        }

        // Detect faces from image url for recognition purpose. This is a helper method for other functions in this quickstart.
        // Parameter `returnFaceId` of `DetectWithUrlAsync` must be set to `true` (by default) for recognition purpose.
        // The field `faceId` in returned `DetectedFace`s will be used in Face - Find Similar, Face - Verify. and Face - Identify.
        // It will expire 24 hours after the detection call.
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 2 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection02);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(url)}`");
            return detectedFaces.ToList();
        }

        /*
		 * FIND SIMILAR
		 * This example will take an image and find a similar one to it in another image.
		 */
        public static async Task FindSimilar(IFaceClient client, string url, string recognition_model)
        {
            Console.WriteLine("========FIND SIMILAR========");
            Console.WriteLine();

            List<string> targetImageFileNames = new List<string>
                                {
                                    "Family1-Dad1.jpg",
                                    "Family1-Daughter1.jpg",
                                    "Family1-Mom1.jpg",
                                    "Family1-Son1.jpg",
                                    "Family2-Lady1.jpg",
                                    "Family2-Man1.jpg",
                                    "Family3-Lady1.jpg",
                                    "Family3-Man1.jpg"
                                };

            string sourceImageFileName = "findsimilar.jpg";
            IList<Guid?> targetFaceIds = new List<Guid?>();
            foreach (var targetImageFileName in targetImageFileNames)
            {
                // Detect faces from target image url.
                var faces = await DetectFaceRecognize(client, $"{url}{targetImageFileName}", recognition_model);
                // Add detected faceId to list of GUIDs.
                targetFaceIds.Add(faces[0].FaceId.Value);
            }

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognition_model);
            Console.WriteLine();

            // Find a similar face(s) in the list of IDs. Comapring only the first in list for testing purposes.
            IList<SimilarFace> similarResults = await client.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, null, targetFaceIds);
            // </snippet_find_similar>
            // <snippet_find_similar_print>
            foreach (var similarResult in similarResults)
            {
                Console.WriteLine($"Faces from {sourceImageFileName} & ID:{similarResult.FaceId} are similar with confidence: {similarResult.Confidence}.");
            }
            Console.WriteLine();
            // </snippet_find_similar_print>
        }

        /*
		 * LARGE PERSON GROUP
		 * The example will create a large person group, retrieve information from it, 
		 * list the Person IDs it contains, and finally delete a large person group.
		 * For simplicity, the same images are used for the regular-sized person group in IDENTIFY FACES of this quickstart.
		 * A large person group is made up of person group persons. 
		 * One person group person is made up of many similar images of that person, which are each PersistedFace objects.
		 */
        public static async Task LargePersonGroup(IFaceClient client, string url, string recognitionModel)
        {
            Console.WriteLine("========LARGE PERSON GROUP========");
            Console.WriteLine();

            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
            new Dictionary<string, string[]>
                { { "Family1-Dad", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
                      { "Family1-Mom", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
                      { "Family1-Son", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } },
                      { "Family1-Daughter", new[] { "Family1-Daughter1.jpg", "Family1-Daughter2.jpg" } },
                      { "Family2-Lady", new[] { "Family2-Lady1.jpg", "Family2-Lady2.jpg" } },
                      { "Family2-Man", new[] { "Family2-Man1.jpg", "Family2-Man2.jpg" } }
                };

            // Create a large person group ID. 
            string largePersonGroupId = Guid.NewGuid().ToString();
            Console.WriteLine($"Create a large person group ({largePersonGroupId}).");

            // Create the large person group
            await client.LargePersonGroup.CreateAsync(largePersonGroupId: largePersonGroupId, name: largePersonGroupId, recognitionModel);

            // Create Person objects from images in our dictionary
            // We'll store their IDs in the process
            List<Guid> personIds = new List<Guid>();
            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);

                Person personLarge = await client.LargePersonGroupPerson.CreateAsync(largePersonGroupId, groupedFace);
                Console.WriteLine();
                Console.WriteLine($"Create a large person group person '{groupedFace}' ({personLarge.PersonId}).");

                // Store these IDs for later retrieval
                personIds.Add(personLarge.PersonId);

                // Add face to the large person group person.
                foreach (var image in personDictionary[groupedFace])
                {
                    Console.WriteLine($"Add face to the person group person '{groupedFace}' from image `{image}`");
                    PersistedFace face = await client.LargePersonGroupPerson.AddFaceFromUrlAsync(largePersonGroupId, personLarge.PersonId,
                        $"{url}{image}", image);
                }
            }

            // Start to train the large person group.
            Console.WriteLine();
            Console.WriteLine($"Train large person group {largePersonGroupId}.");
            await client.LargePersonGroup.TrainAsync(largePersonGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.LargePersonGroup.GetTrainingStatusAsync(largePersonGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
            Console.WriteLine();

            // Now that we have created and trained a large person group, we can retrieve data from it.
            // Get list of persons and retrieve data, starting at the first Person ID in previously saved list.
            IList<Person> persons = await client.LargePersonGroupPerson.ListAsync(largePersonGroupId, start: "");

            Console.WriteLine($"Persisted Face IDs (from {persons.Count} large person group persons): ");
            foreach (Person person in persons)
            {
                foreach (Guid pFaceId in person.PersistedFaceIds)
                {
                    Console.WriteLine($"The person '{person.Name}' has an image with ID: {pFaceId}");
                }
            }
            Console.WriteLine();

            // After testing, delete the large person group, PersonGroupPersons also get deleted.
            await client.LargePersonGroup.DeleteAsync(largePersonGroupId);
            Console.WriteLine($"Deleted the large person group {largePersonGroupId}.");
            Console.WriteLine();
        }

        /*
		* LARGE FACELIST OPERATIONS
		* Create a large face list and adds single-faced images to it, then retrieve data from the faces.
		* Images are used from URLs. Large face lists are preferred for scale, up to 1 million images.
		*/
        public static async Task LargeFaceListOperations(IFaceClient client, string baseUrl)
        {
            Console.WriteLine("======== LARGE FACELIST OPERATIONS========");
            Console.WriteLine();

            const string LargeFaceListId = "mylargefacelistid_001"; // must be lowercase, 0-9, or "_"
            const string LargeFaceListName = "MyLargeFaceListName";
            const int timeIntervalInMilliseconds = 1000; // waiting time in training

            List<string> singleImages = new List<string>
                                {
                                    "Family1-Dad1.jpg",
                                    "Family1-Daughter1.jpg",
                                    "Family1-Mom1.jpg",
                                    "Family1-Son1.jpg",
                                    "Family2-Lady1.jpg",
                                    "Family2-Man1.jpg",
                                    "Family3-Lady1.jpg",
                                    "Family3-Man1.jpg"
                                };

            // Create a large face list
            Console.WriteLine("Creating a large face list...");
            await client.LargeFaceList.CreateAsync(largeFaceListId: LargeFaceListId, name: LargeFaceListName);

            // Add Faces to the LargeFaceList.
            Console.WriteLine("Adding faces to a large face list...");
            foreach (string image in singleImages)
            {
                // Returns a PersistedFace which contains a GUID.
                await client.LargeFaceList.AddFaceFromUrlAsync(largeFaceListId: LargeFaceListId, url: $"{baseUrl}{image}");
            }

            // Training a LargeFaceList is what sets it apart from a regular FaceList.
            // You must train before using the large face list, for example to use the Find Similar operations.
            Console.WriteLine("Training a large face list...");
            await client.LargeFaceList.TrainAsync(LargeFaceListId);

            // Wait for training finish.
            while (true)
            {
                Task.Delay(timeIntervalInMilliseconds).Wait();
                var status = await client.LargeFaceList.GetTrainingStatusAsync(LargeFaceListId);

                if (status.Status == TrainingStatusType.Running)
                {
                    Console.WriteLine($"Training status: {status.Status}");
                    continue;
                }
                else if (status.Status == TrainingStatusType.Succeeded)
                {
                    Console.WriteLine($"Training status: {status.Status}");
                    break;
                }
                else
                {
                    throw new Exception("The train operation has failed!");
                }
            }

            // Print the large face list
            Console.WriteLine();
            Console.WriteLine("Face IDs from the large face list: ");
            Console.WriteLine();
            Parallel.ForEach(
                    await client.LargeFaceList.ListFacesAsync(LargeFaceListId),
                    faceId =>
                    {
                        Console.WriteLine(faceId.PersistedFaceId);
                    }
                );

            // Delete the large face list, for repetitive testing purposes (cannot recreate list with same name).
            await client.LargeFaceList.DeleteAsync(LargeFaceListId);
            Console.WriteLine();
            Console.WriteLine("Deleted the large face list.");
            Console.WriteLine();
        }

        public static async Task DeletePersonGroup(IFaceClient client, String personGroupId)
        {
            await client.PersonGroup.DeleteAsync(personGroupId);
            Console.WriteLine($"Deleted the person group {personGroupId}.");
        }
    }
}